using System.Runtime.Serialization;

namespace ImagePlatform.Wcf.Contracts;

[DataContract]
public sealed class ImageOperationRequest
{
    [DataMember(Order = 1)]
    public ImageOperation Type { get; set; }

    // Resize
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public int? Width { get; set; }

    [DataMember(Order = 3, EmitDefaultValue = false)]
    public int? Height { get; set; }

    // Compress
    [DataMember(Order = 4, EmitDefaultValue = false)]
    public int? Quality { get; set; } // 1..100
}


