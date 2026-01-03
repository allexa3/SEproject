namespace HelloWorldMVC.Models;

public sealed class ImageUploadResultViewModel
{
    public bool UploadSucceeded { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Web path (under wwwroot) to preview the uploaded file.</summary>
    public string? OriginalImageUrl { get; set; }

    /// <summary>Placeholder for later: where the processed image will be served from.</summary>
    public string? ExpectedProcessedImageUrl { get; set; }

    /// <summary>Placeholder for later: a job id/status link.</summary>
    public string? JobId { get; set; }
}


