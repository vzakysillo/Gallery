using ExamAzure.Models;
using ExamAzure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights;

namespace ExamAzure.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBlobService _blob;
        private readonly IContentInspector _inspector;
        private readonly IClock _clock;
        private readonly MiniGalleryOptions _options;
        private readonly ILogger<HomeController> _log;
        private readonly TelemetryClient _ai;

        public HomeController(IBlobService blob, IContentInspector inspector, IClock clock,
            IOptions<MiniGalleryOptions> options, ILogger<HomeController> log, TelemetryClient ai)
        {
            _blob = blob;
            _inspector = inspector;
            _clock = clock;
            _options = options.Value;
            _log = log;
            _ai = ai;
        }

        [HttpGet("/")]
        public async Task<IActionResult> Index(string? q = null)
        {
            //метадані
            //OriginalFileName
            //UploadedAtUtc

            var items = (await _blob.ListLatestAsync(_options.TopN)).ToList();
            items = items
                .OrderByDescending(i => DateTime.Parse(i.Metadata["UploadedAtUtc"]))
                .ToList();

            //items = items
            //    .OrderByDescending(i => i.Metadata["OriginalFileName"])
            //    .ToList();


            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLowerInvariant();
                items = items.Where(i =>
                    (i.Name?.ToLowerInvariant().Contains(q) ?? false) ||
                    (i.Metadata.TryGetValue("OriginalFileName", out var ofn) && ofn.ToLowerInvariant().Contains(q))
                ).ToList();
            }

            var previewTtl = TimeSpan.FromMinutes(_options.PreviewSasTtlMinutes);
            var model = items.Select(i => new GalleryViewModel
            {
                Name = i.Name,
                OriginalFileName = i.Metadata.TryGetValue("OriginalFileName", out var ofn) ? ofn : null,
                SizeBytes = i.SizeBytes,
                LastModified = i.LastModified,
                ContentType = i.ContentType,
                PreviewUri = _blob.GenerateSasUri(i.Name, previewTtl, asAttachment: false).ToString()
            }).ToList();

            return View(model);
        }

        [HttpPost("/upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null)
            {
                TempData["Error"] = "Файл не вибрано.";
                return RedirectToAction("Index");
            }

            var maxBytes = _options.MaxUploadSizeMb * 1024L * 1024L;
            if (file.Length > maxBytes)
            {
                TempData["Error"] = $"Максимальний розмір файлу {_options.MaxUploadSizeMb} MB.";
                return RedirectToAction("Index");
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var detected = await _inspector.InspectContentTypeAsync(ms);
            if (detected == null || !_options.AllowedContentTypes.Contains(detected))
            {
                TempData["Error"] = "Недопустимий тип файлу.";
                _ai.TrackEvent("UploadFailed", new Dictionary<string, string> { { "Reason", "InvalidContentType" } }, null);
                return RedirectToAction("Index");
            }

            var ext = Path.GetExtension(file.FileName);
            var blobName = $"{Guid.NewGuid()}{ext}";
            var metadata = new Dictionary<string, string>
            {
                ["OriginalFileName"] = Path.GetFileName(file.FileName),
                ["UploadedAtUtc"] = _clock.UtcNow.ToString("o"),
                ["SizeBytes"] = file.Length.ToString()
            };

            ms.Position = 0;
            try
            {
                await _blob.UploadAsync(blobName, ms, detected, metadata);
                TempData["Success"] = "Файл завантажено.";
                _ai.TrackEvent("UploadSucceeded", new Dictionary<string, string> { { "Blob", blobName }, { "ContentType", detected } }, null);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Upload failed");
                TempData["Error"] = "Помилка при завантаженні.";
                _ai.TrackException(ex);
                _ai.TrackEvent("UploadFailed", new Dictionary<string, string> { { "Reason", ex.Message } }, null);
            }

            return RedirectToAction("Index");
        }

        [HttpGet("/files/{name}/link")]
        public IActionResult GetLink(string name, bool attachment = false, int? hours = null)
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest();

            if (!_blob.ExistsAsync(name).GetAwaiter().GetResult())
                return NotFound();

            var ttl = TimeSpan.FromHours(hours ?? _options.LinkSasTtlHours);
            var uri = _blob.GenerateSasUri(name, ttl, asAttachment: attachment);
            _ai.TrackEvent("SasIssued", new Dictionary<string, string> { { "Name", name }, { "TtlHours", ttl.TotalHours.ToString() }, { "Disposition", attachment ? "attachment" : "inline" } }, null);

            return Json(new { url = uri.ToString() });
        }


        [HttpGet("/files/{name}/delete")]
        public IActionResult DeleteConfirm(string name)
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest();

            return View("Delete", name);
        }

        [HttpPost("/files/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string name)
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest();

            await _blob.DeleteAsync(name);
            TempData["Success"] = "Файл видалено.";
            return RedirectToAction("Index");
        }
    }
}