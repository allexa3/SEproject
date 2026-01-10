namespace ImagePlatform.Rpc.Models;

public sealed class WorkerProcessResponse
{
    public bool Success { get; set; }
    public string? OutputUri { get; set; }
    public string? ErrorMessage { get; set; }
}