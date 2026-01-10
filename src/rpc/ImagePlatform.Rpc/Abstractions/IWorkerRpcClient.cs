using System.Threading;
using System.Threading.Tasks;
using ImagePlatform.Rpc.Models;

namespace ImagePlatform.Rpc.Abstractions;

public interface IWorkerRpcClient
{
    Task<WorkerProcessResponse> ProcessAsync(WorkerProcessRequest request, CancellationToken cancellationToken = default);
}