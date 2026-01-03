using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HelloWorldMVC.Data;
using HelloWorldMVC.Models;
using HelloWorldMVC.Services.Options;
using ImagePlatform.Wcf.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelloWorldMVC.Services;

/// <summary>
/// Coordinator background dispatcher:
/// - reads queued JobIds from an in-memory channel
/// - loads Job from DB
/// - calls worker via WCF
/// - updates Job status in DB
/// </summary>
public sealed class ImageJobDispatcher : BackgroundService
{
    private readonly ChannelReader<Guid> _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WorkerOptions> _workerOptions;
    private readonly IOptions<HelloWorldMVC.Services.Options.StorageOptions> _storageOptions;
    private readonly ILogger<ImageJobDispatcher> _logger;

    public ImageJobDispatcher(
        Channel<Guid> channel,
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> workerOptions,
        IOptions<HelloWorldMVC.Services.Options.StorageOptions> storageOptions,
        ILogger<ImageJobDispatcher> logger)
    {
        _reader = channel.Reader;
        _scopeFactory = scopeFactory;
        _workerOptions = workerOptions;
        _storageOptions = storageOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var endpoint = new EndpointAddress(_workerOptions.Value.Endpoint);

        var binding = new BasicHttpBinding
        {
            MaxReceivedMessageSize = _storageOptions.Value.MaxUploadBytes
        };

        var timeout = TimeSpan.FromSeconds(Math.Max(1, _workerOptions.Value.TimeoutSeconds));
        binding.OpenTimeout = timeout;
        binding.CloseTimeout = timeout;
        binding.SendTimeout = timeout;
        binding.ReceiveTimeout = timeout;

        var factory = new ChannelFactory<IWorkerWcfService>(binding, endpoint);

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;
            try
            {
                jobId = await _reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var job = await db.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId, stoppingToken);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found in DB.", jobId);
                continue;
            }

            job.Status = JobStatus.Processing;
            job.StartedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(stoppingToken);

            var operations = ParseOperations(job.OperationsJson);

            var req = new ImageJobRequest
            {
                JobId = job.JobId,
                SourceUri = job.SourceUri,
                DestinationUri = job.DestinationUri,
                Operations = operations
            };

            var attempts = Math.Max(1, _workerOptions.Value.RetryCount);
            Exception? lastError = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    var client = factory.CreateChannel();
                    var res = await client.ProcessAsync(req);

                    try
                    {
                        ((ICommunicationObject)client).Close();
                    }
                    catch
                    {
                        ((ICommunicationObject)client).Abort();
                    }

                    if (res.Status == ImageJobStatus.Completed)
                    {
                        job.Status = JobStatus.Completed;
                        job.FinishedAt = DateTimeOffset.UtcNow;
                        job.Error = null;
                        await db.SaveChangesAsync(stoppingToken);
                        lastError = null;
                        break;
                    }

                    job.Status = JobStatus.Failed;
                    job.Error = res.ErrorMessage ?? "Worker failed.";
                    job.FinishedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    lastError = null;
                    break;
                }
                catch (Exception ex) when (ex is EndpointNotFoundException or TimeoutException or CommunicationException)
                {
                    lastError = ex;

                    if (attempt < attempts)
                    {
                        var delay = ComputeBackoffDelay(attempt, _workerOptions.Value.RetryBaseDelayMs);
                        await Task.Delay(delay, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    break;
                }
            }

            if (lastError != null)
            {
                _logger.LogError(lastError, "Failed to dispatch job {JobId} to worker.", jobId);
                job.Status = JobStatus.Failed;
                job.Error = "Worker is not reachable or timed out. Start WorkerHost and try again.";
                job.FinishedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }

    private static TimeSpan ComputeBackoffDelay(int attempt, int baseDelayMs)
    {
        var baseMs = Math.Max(50, baseDelayMs);
        var ms = baseMs * Math.Pow(2, Math.Max(0, attempt - 1));
        ms = Math.Min(ms, 5000); // cap
        return TimeSpan.FromMilliseconds(ms);
    }

    private static List<ImageOperationRequest> ParseOperations(string operationsJson)
    {
        try
        {
            var ops = JsonSerializer.Deserialize<List<JobOperationDto>>(operationsJson) ?? new List<JobOperationDto>();
            var list = new List<ImageOperationRequest>(ops.Count);
            foreach (var op in ops)
            {
                list.Add(new ImageOperationRequest
                {
                    Type = op.Type,
                    Width = op.Width,
                    Height = op.Height,
                    Quality = op.Quality
                });
            }
            return list;
        }
        catch
        {
            return new List<ImageOperationRequest>();
        }
    }
}


