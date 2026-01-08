using System;
using System.Collections.Generic;

namespace ImagePlatform.Rpc.Models;

public sealed class WorkerProcessRequest
{
    public Guid JobId { get; set; }
    public string? SourceUri { get; set; }
    public string? DestinationUri { get; set; }
    public List<ImageTransform> Operations { get; set; } = new();
}