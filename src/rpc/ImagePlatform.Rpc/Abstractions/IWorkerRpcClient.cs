using System.Threading;
using System.Threading.Tasks;
using ImagePlatform.Rpc.Models;

namespace ImagePlatform.Rpc.Abstractions;

/// <summary>
/// Client-side abstraction for calling a worker node using RPC (transport TBD: WCF/gRPC/HTTP).
/// </summary>
public interface IWorkerRpcClient
{
    Task<WorkerProcessResponse> ProcessAsync(WorkerProcessRequest request, CancellationToken cancellationToken = default);
}


