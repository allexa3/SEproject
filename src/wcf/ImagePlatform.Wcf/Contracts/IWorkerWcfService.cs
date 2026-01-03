using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ImagePlatform.Wcf.Contracts;

/// <summary>
/// WCF contract exposed by a worker node. The coordinator can call this remotely to execute a job.
/// </summary>
[ServiceContract]
public interface IWorkerWcfService
{
    [OperationContract]
    Task<ImageJobResult> ProcessAsync(ImageJobRequest request);

    [OperationContract]
    Task<ImageJobResult> GetStatusAsync(Guid jobId);
}


