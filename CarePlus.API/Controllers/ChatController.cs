using CarePlus.API.DTO.ChatDTO;
using CarePlus.API.Services.ChatServices;
using CarePlus.API.Services.ChatServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatServices _chatServices;
    private readonly IChatHistoryServices _chatHistoryServices;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatServices chatServices,
        IChatHistoryServices chatHistoryServices,
        ILogger<ChatController> logger)
    {
        _chatServices = chatServices;
        _chatHistoryServices = chatHistoryServices;
        _logger = logger;
    }

    [HttpGet("threads")]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> ListThreads(CancellationToken cancellationToken)
    {
        if (!TryPatientId(out var patientId))
        {
            return Forbid();
        }

        return Ok(await _chatHistoryServices.ListAsync(patientId, cancellationToken));
    }

    [HttpPost("threads/messages")]
    [Authorize]
    [EnableRateLimiting("chat")]
    [RequestSizeLimit(32_768)]
    public async Task<IActionResult> SendPersisted(
        PersistedChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryPatientId(out var patientId))
        {
            return Forbid();
        }

        try
        {
            var thread = await _chatHistoryServices.SendAsync(patientId, request, cancellationToken);
            return thread == null
                ? NotFound(new { message = "Chat conversation was not found." })
                : Ok(thread);
        }
        catch (ChatRateLimitException exception)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = exception.Message,
                reason = exception.Reason
            });
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            _logger.LogError(exception, "The chatbot could not complete a persisted request.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "The CarePlus assistant is temporarily unavailable. Please try again later."
            });
        }
    }

    [HttpDelete("threads")]
    [Authorize]
    public async Task<IActionResult> DeleteThreads(
        DeleteChatThreadsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryPatientId(out var patientId))
        {
            return Forbid();
        }

        var deleted = await _chatHistoryServices.DeleteAsync(patientId, request.ThreadIds, cancellationToken);
        return Ok(new { deleted });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("chat")]
    [RequestSizeLimit(32_768)]
    public async Task<IActionResult> Send(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _chatServices.AnswerAsync(request, cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            _logger.LogError(exception, "The chatbot could not complete a request.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "The CarePlus assistant is temporarily unavailable. Please try again later."
            });
        }
    }

    [HttpPost("knowledge")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(150_000)]
    public async Task<IActionResult> Index(IndexKnowledgeRequest request, CancellationToken cancellationToken) =>
        Ok(await _chatServices.IndexAsync(request, cancellationToken));

    [HttpPost("knowledge/bundled")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> IndexBundled(CancellationToken cancellationToken) =>
        Ok(await _chatServices.IndexBundledKnowledgeAsync(cancellationToken));

    private bool TryPatientId(out string patientId)
    {
        patientId = User.FindFirstValue("patientId") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(patientId);
    }
}

