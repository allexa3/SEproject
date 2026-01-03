using System;
using System.ComponentModel.DataAnnotations;

namespace HelloWorldMVC.Models;

public sealed class Job
{
    [Key]
    public Guid JobId { get; set; }

    public string? UserId { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Queued;

    [Required]
    public string SourceUri { get; set; } = string.Empty;

    [Required]
    public string DestinationUri { get; set; } = string.Empty;

    /// <summary>
    /// Link used by the UI/API (e.g., /processed/{file}) to view/download output.
    /// </summary>
    public string? OutputUrl { get; set; }

    public string? Error { get; set; }

    /// <summary>
    /// JSON describing requested operations (filters/resize/compress parameters).
    /// </summary>
    public string OperationsJson { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}


