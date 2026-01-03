using System;
using System.Collections.Generic;

namespace ImagePlatform.Rpc.Models;

public sealed record WorkerProcessRequest(
    Guid JobId,
    string SourceUri,
    string DestinationUri,
    IReadOnlyList<ImageTransform> Transforms
);


