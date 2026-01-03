using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ImagePlatform.Wcf.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HelloWorldMVC.Services;

public sealed class ImageJobDispatcher : BackgroundService
{
    private readonly ChannelReader<ImageJob> _reader;
    private readonly InMemoryJobStore _store;
    private readonly ILogger<ImageJobDispatcher> _logger;

    // For now, worker endpoint is fixed to localhost.
    private static readonly EndpointAddress WorkerEndpoint = new("http://localhost:7070/WorkerService.svc");

    public ImageJobDispatcher(Channel<ImageJob> channel, InMemoryJobStore store, ILogger<ImageJobDispatcher> logger)
    {
        _reader = channel.Reader;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // One ChannelFactory for the lifetime of the process.
        var binding = new BasicHttpBinding
        {
            MaxReceivedMessageSize = 10 * 1024 * 1024 // 10MB (match upload limit)
        };

        var factory = new ChannelFactory<IWorkerWcfService>(binding, WorkerEndpoint);

        while (!stoppingToken.IsCancellationRequested)
        {
            ImageJob job;
            try
            {
                job = await _reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _store.MarkProcessing(job.JobId);

            try
            {
                var client = factory.CreateChannel();
                var req = new ImageJobRequest
                {
                    JobId = Guid.Parse(job.JobId),
                    SourceUri = job.SourcePath,
                    DestinationUri = job.DestinationPath
                };

                var res = await client.ProcessAsync(req);

                if (res.Status == ImageJobStatus.Completed)
                {
                    _store.MarkCompleted(job.JobId, job.OutputUrl);
                }
                else
                {
                    _store.MarkFailed(job.JobId, res.ErrorMessage ?? "Worker failed.");
                }

                try
                {
                    ((ICommunicationObject)client).Close();
                }
                catch
                {
                    ((ICommunicationObject)client).Abort();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch image job {JobId} to worker.", job.JobId);
                _store.MarkFailed(job.JobId, "Worker is not reachable. Start WorkerHost and try again.");
            }
        }
    }
}


