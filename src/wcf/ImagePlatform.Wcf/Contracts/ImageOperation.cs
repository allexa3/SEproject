namespace ImagePlatform.Wcf.Contracts;

/// <summary>
/// Requested image operation (high-level; can be expanded as the project grows).
/// </summary>
public enum ImageOperation
{
    Unknown = 0,
    Grayscale = 1,
    Resize = 2,
    Compress = 3
}


