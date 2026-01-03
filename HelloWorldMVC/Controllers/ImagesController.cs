using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Channels;
using HelloWorldMVC.Data;
using HelloWorldMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using HelloWorldMVC.Services.Options;

namespace HelloWorldMVC.Controllers
{
    public class ImagesController : Controller
    {
        private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _db;
        private readonly Channel<Guid> _queue;
        private readonly IOptions<StorageOptions> _storageOptions;

        public ImagesController(IWebHostEnvironment env, AppDbContext db, Channel<Guid> queue, IOptions<StorageOptions> storageOptions)
        {
            _env = env;
            _queue = queue;
            _db = db;
            _storageOptions = storageOptions;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View(new ImageUploadViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ImageUploadViewModel model)
        {
            if (model?.File == null)
            {
                ModelState.AddModelError(nameof(ImageUploadViewModel.File), "Please choose an image file.");
                return View(model ?? new ImageUploadViewModel());
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.File.Length <= 0)
            {
                ModelState.AddModelError(nameof(ImageUploadViewModel.File), "The selected file is empty.");
                return View(model);
            }

            if (model.File.Length > _storageOptions.Value.MaxUploadBytes)
            {
                ModelState.AddModelError(nameof(ImageUploadViewModel.File), "File is too large (max 10MB).");
                return View(model);
            }

            var ext = Path.GetExtension(model.File.FileName)?.ToLowerInvariant() ?? string.Empty;
            var isAllowed = Array.Exists(AllowedExtensions, e => e == ext);
            if (!isAllowed)
            {
                ModelState.AddModelError(nameof(ImageUploadViewModel.File), "Unsupported file type. Please upload png/jpg/jpeg/gif/webp.");
                return View(model);
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, _storageOptions.Value.UploadsSubdir);
            Directory.CreateDirectory(uploadsDir);

            var jobGuid = Guid.NewGuid();
            var jobId = jobGuid.ToString("N");
            var safeFileName = $"{jobId}{ext}";
            var diskPath = Path.Combine(uploadsDir, safeFileName);

            await using (var fs = System.IO.File.Create(diskPath))
            {
                await model.File.CopyToAsync(fs);
            }

            var originalUrl = Url.Content($"~/{_storageOptions.Value.UploadsSubdir}/{safeFileName}");

            // Destination is inside this MVC app's wwwroot so the result can be served to the browser.
            var processedDir = Path.Combine(_env.WebRootPath, _storageOptions.Value.ProcessedSubdir);
            Directory.CreateDirectory(processedDir);
            var processedFileName = $"{jobId}{ext}";
            var destinationPath = Path.Combine(processedDir, processedFileName);
            var expectedProcessedUrl = Url.Content($"~/{_storageOptions.Value.ProcessedSubdir}/{processedFileName}");

            // Store a DB job record and enqueue for dispatch (local demo: coordinator -> worker over WCF).
            var job = new Job
            {
                JobId = jobGuid,
                Status = JobStatus.Queued,
                SourceUri = new Uri(diskPath).AbsoluteUri,
                DestinationUri = new Uri(destinationPath).AbsoluteUri,
                OutputUrl = expectedProcessedUrl,
                OperationsJson = "[]"
            };
            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();
            _queue.Writer.TryWrite(jobGuid);

            return View("Result", new ImageUploadResultViewModel
            {
                UploadSucceeded = true,
                OriginalImageUrl = originalUrl,
                ExpectedProcessedImageUrl = expectedProcessedUrl,
                JobId = jobId
            });
        }

        [HttpGet]
        public IActionResult Status(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return BadRequest(new { error = "jobId is required" });

            if (Guid.TryParse(jobId, out var guid))
            {
                var job = _db.Jobs.AsNoTracking().FirstOrDefault(j => j.JobId == guid);
                if (job != null)
                {
                    return Json(new
                    {
                        jobId = job.JobId.ToString("N"),
                        status = job.Status.ToString(),
                        outputUrl = job.OutputUrl,
                        errorMessage = job.Error
                    });
                }
            }

            return Json(new { jobId, status = "Unknown" });
        }
    }
}


