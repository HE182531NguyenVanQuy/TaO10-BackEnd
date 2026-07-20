using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.DTOs.AiRoadmaps;
using TaO10_BackEnd.Exceptions;
using TaO10_BackEnd.Services;

namespace TaO10_BackEnd.Controllers;

[ApiController]
[Authorize]
[Route("api/ai-roadmaps")]
public class AiRoadmapsController : ControllerBase
{
    private readonly IAiRoadmapService _aiRoadmapService;
    private readonly ILogger<AiRoadmapsController> _logger;
    private readonly PackageAccessService _packageAccessService;

    public AiRoadmapsController(IAiRoadmapService aiRoadmapService, ILogger<AiRoadmapsController> logger, PackageAccessService packageAccessService)
    {
        _aiRoadmapService = aiRoadmapService;
        _logger = logger;
        _packageAccessService = packageAccessService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyRoadmap()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<StudyRoadmapDto>.ErrorResponse("Bạn cần đăng nhập.", "UNAUTHORIZED", 401));
        }

        var roadmap = await _aiRoadmapService.GetRoadmapAsync(userId.Value);
        if (roadmap == null)
        {
            return NotFound(ApiResponse<StudyRoadmapDto>.ErrorResponse("Chưa có lộ trình học.", "ROADMAP_NOT_FOUND", 404));
        }

        return Ok(ApiResponse<StudyRoadmapDto>.SuccessResponse(roadmap, "Roadmap retrieved successfully"));
    }

    [HttpPost("me")]
    public async Task<IActionResult> GenerateMyRoadmap()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<StudyRoadmapDto>.ErrorResponse("Bạn cần đăng nhập.", "UNAUTHORIZED", 401));
        }

        if (!await _packageAccessService.HasActivePackageAsync(userId.Value, PackageAccessService.PracticeMinimumDays))
        {
            return StatusCode(403, ApiResponse<StudyRoadmapDto>.ErrorResponse(
                "Tạo lộ trình học yêu cầu gói từ 3 tháng trở lên.", "PACKAGE_UPGRADE_REQUIRED", 403));
        }

        try
        {
            var roadmap = await _aiRoadmapService.GenerateRoadmapAsync(userId.Value);
            return Ok(ApiResponse<StudyRoadmapDto>.SuccessResponse(roadmap, "Roadmap generated successfully"));
        }
        catch (OpenRouterQuotaExceededException ex)
        {
            _logger.LogWarning(ex, "OpenRouter quota exceeded while generating roadmap for user {UserId}", userId);
            return StatusCode(429, ApiResponse<StudyRoadmapDto>.ErrorResponse(ex.Message, ex.ErrorCode, 429));
        }
        catch (OpenRouterUnavailableException ex)
        {
            _logger.LogWarning(ex, "OpenRouter unavailable while generating roadmap for user {UserId}", userId);
            return StatusCode(503, ApiResponse<StudyRoadmapDto>.ErrorResponse(ex.Message, ex.ErrorCode, 503));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ít nhất 1 lần", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<StudyRoadmapDto>.ErrorResponse(ex.Message, "NO_ATTEMPT_DATA", 400));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AI roadmap generation failed for user {UserId}", userId);
            return BadRequest(ApiResponse<StudyRoadmapDto>.ErrorResponse(ex.Message, "AI_ROADMAP_FAILED", 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI roadmap for user {UserId}", userId);
            return StatusCode(500, ApiResponse<StudyRoadmapDto>.ErrorResponse(ex.Message, "AI_ROADMAP_FAILED", 500));
        }
    }

    private Guid? GetAuthenticatedUserId()
    {
        var userIdValue =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("nameid");

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
