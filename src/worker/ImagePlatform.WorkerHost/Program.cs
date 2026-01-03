using CoreWCF;
using CoreWCF.Configuration;
using ImagePlatform.Tpl.Processing;
using ImagePlatform.Wcf.Contracts;
using ImagePlatform.WorkerHost;

var builder = WebApplication.CreateBuilder(args);

// Default worker URL (can be overridden via --urls or ASPNETCORE_URLS)
builder.WebHost.UseUrls("http://localhost:7070");

builder.Services.AddServiceModelServices();
builder.Services.AddSingleton<ImageProcessingPipeline>();
builder.Services.AddSingleton<WorkerWcfService>();

var app = builder.Build();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder
        .AddService<WorkerWcfService>()
        .AddServiceEndpoint<WorkerWcfService, IWorkerWcfService>(
            new BasicHttpBinding(),
            "/WorkerService.svc");
});

app.MapGet("/", () => "WorkerHost is running. WCF endpoint: /WorkerService.svc");

app.Run();
