using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ImagePlatform.Wcf.Contracts;

[DataContract]
public sealed class ImageJobRequest
{
    [DataMember(Order = 1)]
    public Guid JobId { get; set; } = Guid.NewGuid();

    /// <summary>Where the original image lives (e.g., Azure Blob URL or blob name).</summary>
    [DataMember(Order = 2)]
    public string? SourceUri { get; set; }

    /// <summary>Where the processed image should be written (e.g., Azure Blob URL or blob name).</summary>
    [DataMember(Order = 3)]
    public string? DestinationUri { get; set; }

    [DataMember(Order = 4)]
    public List<ImageOperationRequest> Operations { get; set; } = new();
}


