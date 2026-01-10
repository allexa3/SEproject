using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using ImagePlatform.Rpc.Abstractions;
using ImagePlatform.Rpc.Models;
using ImagePlatform.Wcf.Contracts;

namespace ImagePlatform.Rpc.Wcf;

public sealed class WcfWorkerRpcClient : IWorkerRpcClient
{
    private readonly ChannelFactory<IWorkerWcfService> _factory;

    public WcfWorkerRpcClient(string endpointUrl)
    {
        var binding = new BasicHttpBinding();
        var endpoint = new EndpointAddress(endpointUrl);
        _factory = new ChannelFactory<IWorkerWcfService>(binding, endpoint);
    }

    public async Task<WorkerProcessResponse> ProcessAsync(WorkerProcessRequest request, CancellationToken cancellationToken = default)
    {
        // Map domain model to WCF DataContract
        var wcfRequest = new ImageJobRequest
        {
            JobId = request.JobId,
            SourceUri = request.SourceUri,
            DestinationUri = request.DestinationUri,
            Operations = request.Operations.Select(op => new ImageOperationRequest
            {
                Type = Enum.TryParse<ImageOperation>(op.Name, out var type) ? type : ImageOperation.Unknown,
                Width = op.Parameters.TryGetValue("width", out var w) && int.TryParse(w, out var wv) ? wv : null,
                Height = op.Parameters.TryGetValue("height", out var h) && int.TryParse(h, out var hv) ? hv : null,
                Quality = op.Parameters.TryGetValue("quality", out var q) && int.TryParse(q, out var qv) ? qv : null
            }).ToList()
        };

        var proxy = _factory.CreateChannel();
        try
        {
            var result = await proxy.ProcessAsync(wcfRequest);
            ((ICommunicationObject)proxy).Close();

            return new WorkerProcessResponse
            {
                Success = result.Status == ImageJobStatus.Completed,
                OutputUri = result.OutputUri,
                ErrorMessage = result.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            ((ICommunicationObject)proxy).Abort();
            return new WorkerProcessResponse
            {
                Success = false,
                ErrorMessage = $"RPC communication failed: {ex.Message}"
            };
        }
    }
}