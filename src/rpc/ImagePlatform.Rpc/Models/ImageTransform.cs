namespace ImagePlatform.Rpc.Models;

public record ImageTransform(
    string Name,
    System.Collections.Generic.Dictionary<string, string> Parameters);