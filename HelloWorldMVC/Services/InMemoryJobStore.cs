using System;
using System.Collections.Concurrent;

namespace HelloWorldMVC.Services;

public sealed class InMemoryJobStore
{
    private readonly ConcurrentDictionary<string, ImageJobStatusDto> _jobs = new(StringComparer.OrdinalIgnoreCase);

    public ImageJobStatusDto CreateQueued(string jobId, string outputUrl)
    {
        var dto = new ImageJobStatusDto
        {
            JobId = jobId,
            Status = "Queued",
            OutputUrl = outputUrl
        };
        _jobs[jobId] = dto;
        return dto;
    }

    public bool TryGet(string jobId, out ImageJobStatusDto dto) => _jobs.TryGetValue(jobId, out dto!);

    public void MarkProcessing(string jobId)
    {
        _jobs.AddOrUpdate(jobId,
            addValueFactory: _ => new ImageJobStatusDto { JobId = jobId, Status = "Processing" },
            updateValueFactory: (_, existing) =>
            {
                existing.Status = "Processing";
                return existing;
            });
    }

    public void MarkCompleted(string jobId, string outputUrl)
    {
        _jobs.AddOrUpdate(jobId,
            addValueFactory: _ => new ImageJobStatusDto { JobId = jobId, Status = "Completed", OutputUrl = outputUrl },
            updateValueFactory: (_, existing) =>
            {
                existing.Status = "Completed";
                existing.OutputUrl = outputUrl;
                existing.ErrorMessage = null;
                return existing;
            });
    }

    public void MarkFailed(string jobId, string errorMessage)
    {
        _jobs.AddOrUpdate(jobId,
            addValueFactory: _ => new ImageJobStatusDto { JobId = jobId, Status = "Failed", ErrorMessage = errorMessage },
            updateValueFactory: (_, existing) =>
            {
                existing.Status = "Failed";
                existing.ErrorMessage = errorMessage;
                return existing;
            });
    }
}


