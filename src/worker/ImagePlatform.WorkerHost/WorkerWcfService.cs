using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImagePlatform.Tpl.Processing;
using ImagePlatform.Wcf.Contracts;

namespace ImagePlatform.WorkerHost;

/// <summary>
/// Minimal worker implementation: reads an input file, simulates processing, writes output file.
/// </summary>
public sealed class WorkerWcfService : IWorkerWcfService
{
    private static readonly ConcurrentDictionary<Guid, ImageJobResult> _results = new();
    private readonly ImageProcessingPipeline _pipeline;

    public WorkerWcfService(ImageProcessingPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task<ImageJobResult> ProcessAsync(ImageJobRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var jobId = request.JobId;
        _results[jobId] = new ImageJobResult { JobId = jobId, Status = ImageJobStatus.Processing };

        try
        {
            if (string.IsNullOrWhiteSpace(request.SourceUri))
                throw new ArgumentException("SourceUri is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.DestinationUri))
                throw new ArgumentException("DestinationUri is required.", nameof(request));

            var sourcePath = request.SourceUri;
            var destinationPath = request.DestinationUri;

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Source image not found.", sourcePath);

            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destDir))
                Directory.CreateDirectory(destDir);

            var bytes = await File.ReadAllBytesAsync(sourcePath);

            // Minimal "processing": pass through the TPL pipeline (currently simulated delays)
            var transforms = request.Operations.Select(op => new ImageTransform(op.ToString())).ToList();
            var processed = await _pipeline.ProcessImageAsync(bytes, transforms);

            await File.WriteAllBytesAsync(destinationPath, processed);

            var result = new ImageJobResult
            {
                JobId = jobId,
                Status = ImageJobStatus.Completed,
                OutputUri = destinationPath
            };

            _results[jobId] = result;
            return result;
        }
        catch (Exception ex)
        {
            var failed = new ImageJobResult
            {
                JobId = jobId,
                Status = ImageJobStatus.Failed,
                ErrorMessage = ex.Message
            };

            _results[jobId] = failed;
            return failed;
        }
    }

    public Task<ImageJobResult> GetStatusAsync(Guid jobId)
    {
        if (_results.TryGetValue(jobId, out var result))
            return Task.FromResult(result);

        return Task.FromResult(new ImageJobResult { JobId = jobId, Status = ImageJobStatus.Unknown });
    }
}


