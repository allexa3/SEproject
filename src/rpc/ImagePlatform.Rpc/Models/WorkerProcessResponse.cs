using System;

namespace ImagePlatform.Rpc.Models;

public sealed record WorkerProcessResponse(
    Guid JobId,
    bool Success,
    string? OutputUri = null,
    string? ErrorMessage = null
);


