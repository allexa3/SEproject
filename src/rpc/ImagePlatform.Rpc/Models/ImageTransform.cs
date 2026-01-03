using System.Collections.Generic;

namespace ImagePlatform.Rpc.Models;

/// <summary>
/// Transport-agnostic description of an image operation for RPC calls.
/// </summary>
public sealed record ImageTransform(
    string Name,
    IReadOnlyDictionary<string, string>? Parameters = null
);


