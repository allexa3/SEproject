using System;

namespace HelloWorldMVC.Services;

public sealed class ImageJob
{
    public string JobId { get; init; } = Guid.NewGuid().ToString("N");
    public string SourcePath { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public string OutputUrl { get; init; } = string.Empty;
}


