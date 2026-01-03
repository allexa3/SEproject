using System.Collections.Generic;

namespace ImagePlatform.Tpl.Processing;

/// <summary>
/// Simple transform description used by the TPL pipeline.
/// Note: actual image manipulation will be implemented later (e.g., via ImageSharp).
/// </summary>
public sealed record ImageTransform(
    string Name,
    IReadOnlyDictionary<string, string>? Parameters = null
);


