using System.Threading;
using System.Threading.Tasks;
using ImagePlatform.Rpc.Models;

namespace ImagePlatform.Rpc.Abstractions;

/// <summary>
/// Server-side abstraction implemented by a worker node.
/// </summary>
public interface IWorkerRpcServer
{
    Task<WorkerProcessResponse> ProcessAsync(WorkerProcessRequest request, CancellationToken cancellationToken = default);
}


