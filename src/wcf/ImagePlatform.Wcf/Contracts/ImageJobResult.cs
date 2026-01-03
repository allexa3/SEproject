using System;
using System.Runtime.Serialization;

namespace ImagePlatform.Wcf.Contracts;

[DataContract]
public sealed class ImageJobResult
{
    [DataMember(Order = 1)]
    public Guid JobId { get; set; }

    [DataMember(Order = 2)]
    public ImageJobStatus Status { get; set; }

    [DataMember(Order = 3)]
    public string? OutputUri { get; set; }

    [DataMember(Order = 4)]
    public string? ErrorMessage { get; set; }
}


