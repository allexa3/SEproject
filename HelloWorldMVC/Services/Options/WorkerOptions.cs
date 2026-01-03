namespace HelloWorldMVC.Services.Options;

public sealed class WorkerOptions
{
    public string Endpoint { get; set; } = "http://localhost:7070/WorkerService.svc";
    public int TimeoutSeconds { get; set; } = 15;
    public int RetryCount { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 250;
}


