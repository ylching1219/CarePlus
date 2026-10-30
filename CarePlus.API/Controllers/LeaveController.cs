using CarePlus.API.DTO.LeaveDTO;
using CarePlus.API.Services.LeaveServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveServices _leaveServices;
        private readonly IWebHostEnvironment _environment;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png",
                ".doc",
                ".docx"
            };

        public LeaveController(
            ILeaveServices leaveServices,
            IWebHostEnvironment environment)
        {
            _leaveServices = leaveServices;
            _environment = environment;
        }

        [HttpPost("Create")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> CreateLeave(
            [FromForm] LeaveCreateFormRequest request)
        {
            var file = request.SupportDocument;

            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "Support document is required.", 400));
            }

            var extension = Path.GetExtension(file.FileName);

            if (file.Length > 10 * 1024 * 1024 || !AllowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "Upload a PDF, JPG, PNG, DOC, or DOCX file under 10 MB.", 400));
            }

            var root = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var folder = Path.Combine(root, "leave-documents");
            Directory.CreateDirectory(folder);

            var originalName = Path.GetFileName(file.FileName);
            var baseName = Path.GetFileNameWithoutExtension(originalName);
            var invalidCharacters = new string(Path.GetInvalidFileNameChars());
            var safeBaseName = string.Concat(baseName.Select(character =>
                invalidCharacters.Contains(character) ? '_' : character));
            safeBaseName = Regex.Replace(safeBaseName.Trim(), @"\s+", "-");
            safeBaseName = string.IsNullOrWhiteSpace(safeBaseName)
                ? "support-document"
                : safeBaseName;
            safeBaseName = safeBaseName.Length > 100
                ? safeBaseName[..100]
                : safeBaseName;

            var storedName = $"{Guid.NewGuid():N}-{safeBaseName}{extension.ToLowerInvariant()}";
            var storedPath = Path.Combine(folder, storedName);
            var documentReference = $"leave-documents/{storedName}";

            await using (var stream = System.IO.File.Create(storedPath))
            {
                await file.CopyToAsync(stream);
            }

            var result = await _leaveServices.CreateLeaveAsync(new LeaveCreateRequest
            {
                StaffId = request.StaffId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason,
                SupportDocument = documentReference
            });

            if (!result.IsSuccess && System.IO.File.Exists(storedPath))
            {
                System.IO.File.Delete(storedPath);
            }

            return Ok(result);
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> RetrieveLeaves()
        {
            return Ok(await _leaveServices.RetrieveAllLeavesAsync());
        }

        [HttpGet("RetrieveById/{leaveId}")]
        public async Task<IActionResult> RetrieveLeaveById(string leaveId)
        {
            return Ok(await _leaveServices.RetrieveLeaveByIdAsync(leaveId));
        }

        [HttpGet("RetrieveByStaffId/{staffId}")]
        public async Task<IActionResult> RetrieveLeavesByStaffId(string staffId)
        {
            return Ok(await _leaveServices
                .RetrieveLeavesByStaffIdAsync(staffId));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateStatus")]
        public async Task<IActionResult> UpdateLeaveStatus(
            LeaveStatusUpdateRequest request)
        {
            return Ok(await _leaveServices
                .UpdateLeaveStatusAsync(request));
        }
    }
}
