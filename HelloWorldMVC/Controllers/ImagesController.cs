using System;
using System.IO;
using System.Threading.Tasks;
using HelloWorldMVC.Models;
using HelloWorldMVC.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace HelloWorldMVC.Controllers
{
    public class ImagesController : Controller
    {
        private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];
        private const long MaxUploadBytes = 10 * 1024 * 1024; // 10 MB (basic guardrail)

        private readonly IWebHostEnvironment _env;
        private readonly InMemoryJobStore _store;
        private readonly Channel<ImageJob> _queue;

        public ImagesController(IWebHostEnvironment env, InMemoryJobStore store, Channel<ImageJob> queue)
        {
            _env = env;
            _store = store;
            _queue = queue;
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

            if (model.File.Length > MaxUploadBytes)
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

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var jobId = Guid.NewGuid().ToString("N");
            var safeFileName = $"{jobId}{ext}";
            var diskPath = Path.Combine(uploadsDir, safeFileName);

            await using (var fs = System.IO.File.Create(diskPath))
            {
                await model.File.CopyToAsync(fs);
            }

            var originalUrl = Url.Content($"~/uploads/{safeFileName}");

            // Destination is inside this MVC app's wwwroot so the result can be served to the browser.
            var processedDir = Path.Combine(_env.WebRootPath, "processed");
            Directory.CreateDirectory(processedDir);
            var processedFileName = $"{jobId}{ext}";
            var destinationPath = Path.Combine(processedDir, processedFileName);
            var expectedProcessedUrl = Url.Content($"~/processed/{processedFileName}");

            // Enqueue a "distributed" job (local demo: coordinator -> worker over WCF).
            _store.CreateQueued(jobId, expectedProcessedUrl);
            _queue.Writer.TryWrite(new ImageJob
            {
                JobId = jobId,
                SourcePath = diskPath,
                DestinationPath = destinationPath,
                OutputUrl = expectedProcessedUrl
            });

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

            if (_store.TryGet(jobId, out var dto))
                return Json(dto);

            return Json(new ImageJobStatusDto { JobId = jobId, Status = "Unknown" });
        }
    }
}


