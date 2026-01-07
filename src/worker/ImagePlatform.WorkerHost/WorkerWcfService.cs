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

            var sourcePath = ResolveToLocalPath(request.SourceUri);
            var destinationPath = ResolveToLocalPath(request.DestinationUri);

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Source image not found.", sourcePath);

            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destDir))
                Directory.CreateDirectory(destDir);

            var bytes = await File.ReadAllBytesAsync(sourcePath);

            // Minimal "processing": pass through the TPL pipeline (currently simulated delays)
           var transforms = request.Operations.Select(op =>
    new ImageTransform(
        Name: op.Type.ToString(),
        Parameters: new System.Collections.Generic.Dictionary<string, string?>
        {
            ["width"] = op.Width?.ToString(),
            ["height"] = op.Height?.ToString(),
            ["quality"] = op.Quality?.ToString()
        }
        .Where(kv => kv.Value != null)
        .ToDictionary(kv => kv.Key, kv => kv.Value!)))
    .ToList();

// FALLBACK: dacă UI nu trimite operații, aplicăm unele default
if (transforms.Count == 0)
{
    transforms.Add(new ImageTransform(
        "resize",
        new System.Collections.Generic.Dictionary<string, string>
        {
            ["width"] = "300"
        }));

    transforms.Add(new ImageTransform("grayscale"));

    transforms.Add(new ImageTransform(
        "compress",
        new System.Collections.Generic.Dictionary<string, string>
        {
            ["quality"] = "70"
        }));
}

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

    private static string ResolveToLocalPath(string uriOrPath)
    {
        if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                return uri.LocalPath;

            throw new InvalidOperationException($"Unsupported URI scheme '{uri.Scheme}'. Expected file:// or a local path.");
        }

        return uriOrPath;
    }

    public Task<ImageJobResult> GetStatusAsync(Guid jobId)
    {
        if (_results.TryGetValue(jobId, out var result))
            return Task.FromResult(result);

        return Task.FromResult(new ImageJobResult { JobId = jobId, Status = ImageJobStatus.Unknown });
    }
}


