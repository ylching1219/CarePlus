using System.Security.Claims;
using CarePlus.API.DTO.LeaveDTO;
using CarePlus.API.Extension;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.LeaveServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveServices _leaveServices;
        private readonly IStaffRepository _staffRepository;
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
            IStaffRepository staffRepository,
            IWebHostEnvironment environment)
        {
            _leaveServices = leaveServices;
            _staffRepository = staffRepository;
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

            var callerStaffId = User.FindFirstValue("staffId");
            var isAdmin = User.IsInRole("Admin");
            var staffId = isAdmin ? request.StaffId : callerStaffId;
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            if (!isAdmin && !string.Equals(request.StaffId, staffId, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<object>.Fail("You can only create leave for your own account.", 403));
            }

            var staff = await _staffRepository.GetStaffByIdAsync(staffId);
            if (staff == null)
            {
                return NotFound(ApiResponse<object>.Fail("Staff not found.", 404));
            }

            var root = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var folder = Path.Combine(root, "leave-documents");
            Directory.CreateDirectory(folder);

            var storedName = UploadFileNameHelper.GenerateOpaque(extension);
            var storedPath = Path.Combine(folder, storedName);
            var documentReference = $"leave-documents/{storedName}";

            await using (var stream = System.IO.File.Create(storedPath))
            {
                await file.CopyToAsync(stream);
            }

            var result = await _leaveServices.CreateLeaveAsync(new LeaveCreateRequest
            {
                StaffId = staffId,
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
            if (User.IsInRole("Admin"))
            {
                return Ok(await _leaveServices.RetrieveAllLeavesAsync());
            }

            var staffId = User.FindFirstValue("staffId");
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _leaveServices.RetrieveLeavesByStaffIdAsync(staffId));
        }

        [HttpGet("RetrieveById/{leaveId}")]
        public async Task<IActionResult> RetrieveLeaveById(string leaveId)
        {
            var result = await _leaveServices.RetrieveLeaveByIdAsync(leaveId);
            if (!result.IsSuccess || result.Data is not LeaveRetrieveResponse leave)
            {
                return Ok(result);
            }

            if (!User.IsInRole("Admin"))
            {
                var staffId = User.FindFirstValue("staffId");
                if (!string.Equals(leave.StaffId, staffId, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        ApiResponse<object>.Fail("You can only view your own leave requests.", 403));
                }
            }

            return Ok(result);
        }

        [HttpGet("RetrieveByStaffId/{staffId}")]
        public async Task<IActionResult> RetrieveLeavesByStaffId(string staffId)
        {
            if (!User.IsInRole("Admin"))
            {
                var callerStaffId = User.FindFirstValue("staffId");
                if (!string.Equals(callerStaffId, staffId, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        ApiResponse<object>.Fail("You can only view your own leave requests.", 403));
                }
            }

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
