using ImagePlatform.Wcf.Contracts;

namespace HelloWorldMVC.Services;

/// <summary>
/// Serializable representation of an operation request stored in Job.OperationsJson.
/// </summary>
public sealed class JobOperationDto
{
    public ImageOperation Type { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Quality { get; set; }
}


