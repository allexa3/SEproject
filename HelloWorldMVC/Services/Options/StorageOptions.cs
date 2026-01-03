namespace HelloWorldMVC.Services.Options;

public sealed class StorageOptions
{
    public string UploadsSubdir { get; set; } = "uploads";
    public string ProcessedSubdir { get; set; } = "processed";
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
}


