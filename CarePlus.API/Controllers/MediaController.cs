using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    // ponytail: replaces anonymous UseStaticFiles for profiles / leave docs
    [ApiController]
    [Route("api")]
    public class MediaController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public MediaController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [Authorize]
        [HttpGet("profiles/{fileName}")]
        public IActionResult ProfilePhoto(string fileName)
        {
            return ServeFile("profiles", fileName);
        }

        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        [HttpGet("leave-documents/{fileName}")]
        public IActionResult LeaveDocument(string fileName)
        {
            return ServeFile("leave-documents", fileName);
        }

        private IActionResult ServeFile(string folderName, string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeName) || safeName != fileName)
            {
                return NotFound();
            }

            var root = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var path = Path.GetFullPath(Path.Combine(root, folderName, safeName));
            var folder = Path.GetFullPath(Path.Combine(root, folderName));
            if (!path.StartsWith(folder, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path))
            {
                return NotFound();
            }

            return PhysicalFile(path, ContentType(safeName));
        }

        private static string ContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream",
            };
        }
    }
}
