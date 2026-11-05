using CarePlus.API.DTO.ChatDTO;
using CarePlus.API.Services.ChatServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatServices _chatServices;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatServices chatServices, ILogger<ChatController> logger)
    {
        _chatServices = chatServices;
        _logger = logger;
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
}

