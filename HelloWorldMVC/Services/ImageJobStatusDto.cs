namespace HelloWorldMVC.Services;

public sealed class ImageJobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public string? OutputUrl { get; set; }
    public string? ErrorMessage { get; set; }
}


