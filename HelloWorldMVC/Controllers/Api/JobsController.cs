using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HelloWorldMVC.Data;
using HelloWorldMVC.Models;
using HelloWorldMVC.Services;
using HelloWorldMVC.Services.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelloWorldMVC.Controllers.Api
{
    [ApiController]
    [Route("api/jobs")]
    public sealed class JobsController : ControllerBase
    {
        private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _db;
        private readonly Channel<Guid> _queue;
        private readonly IOptions<StorageOptions> _storageOptions;

        public JobsController(
            IWebHostEnvironment env,
            AppDbContext db,
            Channel<Guid> queue,
            IOptions<StorageOptions> storageOptions)
        {
            _env = env;
            _db = db;
            _queue = queue;
            _storageOptions = storageOptions;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateJob(
            [FromForm] IFormFile file,
            [FromForm] string? userId,
            [FromForm] string? operationsJson,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length <= 0)
                return BadRequest(new { error = "file is required" });

            if (file.Length > _storageOptions.Value.MaxUploadBytes)
                return BadRequest(new { error = $"file too large (max {_storageOptions.Value.MaxUploadBytes} bytes)" });

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { error = "unsupported file type (png/jpg/jpeg/gif/webp)" });

            // Validate operations json (optional). Store normalized JSON string.
            var normalizedOpsJson = NormalizeOperationsJson(operationsJson);
            if (normalizedOpsJson == null)
                return BadRequest(new { error = "operationsJson must be a JSON array of operations" });

            var uploadsDir = Path.Combine(_env.WebRootPath, _storageOptions.Value.UploadsSubdir);
            var processedDir = Path.Combine(_env.WebRootPath, _storageOptions.Value.ProcessedSubdir);
            Directory.CreateDirectory(uploadsDir);
            Directory.CreateDirectory(processedDir);

            var jobId = Guid.NewGuid();
            var jobIdStr = jobId.ToString("N");

            var uploadFileName = $"{jobIdStr}{ext}";
            var uploadPath = Path.Combine(uploadsDir, uploadFileName);

            await using (var fs = System.IO.File.Create(uploadPath))
            {
                await file.CopyToAsync(fs, cancellationToken);
            }

            var processedFileName = $"{jobIdStr}{ext}";
            var processedPath = Path.Combine(processedDir, processedFileName);

            var outputUrl = Url.Content($"~/{_storageOptions.Value.ProcessedSubdir}/{processedFileName}");

            var job = new Job
            {
                JobId = jobId,
                UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                Status = JobStatus.Queued,
                SourceUri = new Uri(uploadPath).AbsoluteUri,
                DestinationUri = new Uri(processedPath).AbsoluteUri,
                OutputUrl = outputUrl,
                OperationsJson = normalizedOpsJson
            };

            _db.Jobs.Add(job);
            await _db.SaveChangesAsync(cancellationToken);
            _queue.Writer.TryWrite(jobId);

            return CreatedAtAction(nameof(GetJob), new { id = jobIdStr }, new
            {
                jobId = jobIdStr,
                status = job.Status.ToString(),
                outputUrl,
                downloadUrl = Url.ActionLink(nameof(Download), values: new { id = jobIdStr })
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJob(string id, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(id, out var jobId))
                return NotFound();

            var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);
            if (job == null) return NotFound();

            return Ok(new
            {
                jobId = job.JobId.ToString("N"),
                userId = job.UserId,
                status = job.Status.ToString(),
                createdAt = job.CreatedAt,
                startedAt = job.StartedAt,
                finishedAt = job.FinishedAt,
                error = job.Error,
                outputUrl = job.OutputUrl,
                downloadUrl = Url.ActionLink(nameof(Download), values: new { id = job.JobId.ToString("N") })
            });
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(string id, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(id, out var jobId))
                return NotFound();

            var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);
            if (job == null) return NotFound();

            if (job.Status != JobStatus.Completed)
                return Conflict(new { error = $"job not completed (status={job.Status})" });

            var path = ResolveToLocalPath(job.DestinationUri);
            if (!System.IO.File.Exists(path))
                return NotFound(new { error = "output file missing" });

            // Best-effort content type
            var contentType = "application/octet-stream";
            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        private static string? NormalizeOperationsJson(string? operationsJson)
        {
            if (string.IsNullOrWhiteSpace(operationsJson))
                return "[]";

            try
            {
                var ops = JsonSerializer.Deserialize<JobOperationDto[]>(operationsJson);
                if (ops == null) return "[]";
                return JsonSerializer.Serialize(ops);
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveToLocalPath(string uriOrPath)
        {
            if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                    return uri.LocalPath;
            }

            return uriOrPath;
        }
    }
}


