# SEproject — Distributed Image Processing Platform

This repository is a starter structure for a distributed image-processing platform (based on `task.txt`).

## File system guide (what is where)

### Solution root
- **`ImagePlatform.sln`**: the main solution (open this in Visual Studio / Rider).
- **`task.txt`**: the original project concept/requirements.
- **`scripts/`**: Windows helper scripts to run the demo and fix common port/process issues.

### Coordinator (web app)
- **`HelloWorldMVC/`**: ASP.NET Core MVC app acting as the **Coordinator/API**.
  - **Upload UI**: `HelloWorldMVC/Controllers/ImagesController.cs` + `HelloWorldMVC/Views/Images/*`
  - **In-memory job queue + status** (demo only): `HelloWorldMVC/Services/*`
  - **Default route** points to the upload page (so `/` shows upload).
  - **Files written locally**:
    - `HelloWorldMVC/wwwroot/uploads/` → original uploads
    - `HelloWorldMVC/wwwroot/processed/` → processed output (demo stand‑in for Blob Storage)

### Worker (WCF server)
- **`src/worker/ImagePlatform.WorkerHost/`**: separate process that acts as a **worker node**.
  - Entry point: `src/worker/ImagePlatform.WorkerHost/Program.cs`
  - WCF service implementation: `src/worker/ImagePlatform.WorkerHost/WorkerWcfService.cs`
  - Exposes a CoreWCF endpoint: `http://localhost:7070/WorkerService.svc`

### Shared “tool-owned” libraries (4 teammates)
- **WCF contracts**: `src/wcf/ImagePlatform.Wcf/`
  - Shared service contract + DTOs (`ImageJobRequest`, `ImageJobResult`, etc.)
- **TPL helpers**: `src/tpl/ImagePlatform.Tpl/`
  - Worker-side processing helpers (`ImageProcessingPipeline`)
- **Security / Key Vault**: `src/security/ImagePlatform.Security/`
  - Secret abstraction + Key Vault implementation (`ISecretProvider`, `KeyVaultSecretProvider`)
- **RPC abstractions**: `src/rpc/ImagePlatform.Rpc/`
  - Transport‑agnostic RPC interfaces + DTOs (future-proofing for non-WCF transports)

## Project layout (4 teammate-owned areas)

- **Web app (Coordinator/API)**: `HelloWorldMVC/`
  - Current MVC app (targets **.NET 9**).
  - Will later handle uploads/auth/job submission/result retrieval.
- **WCF**: `src/wcf/ImagePlatform.Wcf/`
  - WCF contracts used for coordinator ↔ worker communication.
- **TPL**: `src/tpl/ImagePlatform.Tpl/`
  - Worker-side parallel processing helpers (Task Parallel Library).
- **Security (Key Vault)**: `src/security/ImagePlatform.Security/`
  - Secret abstraction + Key Vault-backed implementation.
- **RPC**: `src/rpc/ImagePlatform.Rpc/`
  - Transport-agnostic RPC DTOs + client/server interfaces (WCF/gRPC/HTTP can implement this later).

## Build / run

Build everything:

```bash
dotnet build ImagePlatform.sln
```

### Demo: coordinator + worker (local “distributed” processing)

1) Run the worker (CoreWCF service):

```bash
dotnet run --project src/worker/ImagePlatform.WorkerHost/ImagePlatform.WorkerHost.csproj --urls http://localhost:7070
```

2) Run the MVC app (Coordinator):

```bash
dotnet run --project HelloWorldMVC/HelloWorldMVC.csproj --urls http://localhost:5163
```

3) Open the UI:
- Upload page: `http://localhost:5163/` (or `http://localhost:5163/Images/Upload`)

What happens:
- Upload saves the original under `HelloWorldMVC/wwwroot/uploads/`
- A job is queued in-memory
- The coordinator calls the worker over WCF (`http://localhost:7070/WorkerService.svc`)
- The worker writes the “processed” output under `HelloWorldMVC/wwwroot/processed/`
- The result page polls `/Images/Status` until the output is ready

Tip (Windows): if you see MSB3027/MSB3026 “file is locked”, it means you already have the app running.
You can use:

```powershell
.\scripts\run-demo.ps1
```

### Run the demo with the script (recommended on Windows)

From repo root in PowerShell:

```powershell
.\scripts\run-demo.ps1
```

What the script does:
- Stops any running `HelloWorldMVC` / `ImagePlatform.WorkerHost` processes
- Frees the default ports (7070 for worker, 5163 for MVC)
- Starts:
  - WorkerHost → `http://localhost:7070`
  - MVC app → `http://localhost:5163`
- Prints the URL you should open

If you see “address already in use” for port 5163:

```powershell
.\scripts\free-port.ps1 -Port 5163
```

## What to build next (toward a complete working app)

Right now, this is a **local demo**: the coordinator and worker run on the same machine, jobs are stored in-memory, and “processing” is a placeholder.

To make it a real distributed platform (aligned with `task.txt`), implement:

### Storage (Azure Blob)
- Store original + processed images in **Azure Blob Storage** instead of local `wwwroot/`.
- Save only blob URLs/keys in the database.

### Queue (Azure Queue Storage / Service Bus)
- Replace the in-memory `Channel<ImageJob>` with **Azure Queue** or **Service Bus**.
- Coordinator: enqueue a message (jobId + source blob + operations + destination blob).
- Worker(s): pull messages, process, update status.

### Job tracking (DB)
- Add a **Jobs** table (JobId, Status, CreatedAt, StartedAt, FinishedAt, Error, OutputUri).
- Result page should poll a real persisted status (not in-memory).

### Real image processing
- Replace the placeholder pipeline with **real transforms** using e.g. `SixLabors.ImageSharp`
  - resize, compress, grayscale, etc.
- Use **TPL** inside the worker to process multiple jobs concurrently with bounded parallelism.

### Security (Key Vault)
- Move connection strings, queue/blob keys, and any encryption keys into **Azure Key Vault**.
- Use `DefaultAzureCredential` in production and user secrets/dev settings locally.

### RPC/WCF hardening
- Add timeouts, retries, and circuit-breaker style behavior around worker calls.
- Consider moving from direct WCF calls to “queue-only” workers for better scalability.

### Auth + UX
- Add authentication (login) and user ownership of jobs.
- Add an operations selector UI (choose resize/compress/grayscale) and show progress + history.