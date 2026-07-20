using Microsoft.AspNetCore.Mvc;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.DTOs.Exams;
using TaO10_BackEnd.Exceptions;
using TaO10_BackEnd.Services;
using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TaO10_BackEnd.Controllers;

/// <summary>
/// Controller for exam operations (read-only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly ILogger<ExamsController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly PackageAccessService _packageAccessService;

    /// <summary>
    /// Initializes a new instance of the ExamsController class
    /// </summary>
    public ExamsController(
        IExamService examService,
        ILogger<ExamsController> logger,
        AppDbContext dbContext,
        IWebHostEnvironment environment,
        PackageAccessService packageAccessService)
    {
        _examService = examService;
        _logger = logger;
        _dbContext = dbContext;
        _environment = environment;
        _packageAccessService = packageAccessService;
    }

    [Authorize]
    [HttpGet("{id}/word")]
    public async Task<IActionResult> DownloadExamWord(Guid id)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userId)) return Unauthorized();
        if (!await _packageAccessService.HasActivePackageAsync(userId, PackageAccessService.WordDownloadMinimumDays))
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(
                "Tính năng tải Word yêu cầu gói từ 6 tháng trở lên.", "PACKAGE_UPGRADE_REQUIRED", 403));
        }
        var examIds = await _dbContext.Exams
            .AsNoTracking()
            .Where(exam =>
                exam.Status.EntityType == AppStatusCodes.EntityTypes.Exam &&
                exam.Status.Code == AppStatusCodes.Exams.Active)
            .OrderByDescending(exam => exam.CreatedAt)
            .ThenBy(exam => exam.ExamId)
            .Select(exam => exam.ExamId)
            .ToListAsync();

        var zeroBasedIndex = examIds.IndexOf(id);
        if (zeroBasedIndex < 0)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy đề thi", "EXAM_NOT_FOUND", 404));
        }

        // DB index 1/21/41 -> Đề 1; 2/22/42 -> Đề 2; ...; 20/40/60 -> Đề 20.
        var wordNumber = zeroBasedIndex % 20 + 1;
        var filePath = Path.Combine(_environment.ContentRootPath, "Exam", $"Đề {wordNumber}.docx");
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("Exam Word template was not found: {FilePath}", filePath);
            return NotFound(ApiResponse<object>.ErrorResponse("File Word của đề chưa có sẵn", "EXAM_WORD_NOT_FOUND", 404));
        }

        return PhysicalFile(
            filePath,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"De-thi-{zeroBasedIndex + 1}.docx",
            enableRangeProcessing: true);
    }

    /// <summary>
    /// Gets all active exams with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>List of exams</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllExams([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("GetAllExams called with pageNumber: {PageNumber}, pageSize: {PageSize}", pageNumber, pageSize);

            if (pageNumber < 1 || pageSize < 1)
            {
                var errorResponse = ApiResponse<List<ExamDto>>.ErrorResponse(
                    "Page number and page size must be greater than 0",
                    "INVALID_PAGINATION",
                    400);
                return BadRequest(errorResponse);
            }

            var (exams, totalCount) = await _examService.GetAllExamsAsync(pageNumber, pageSize);

            var response = new
            {
                exams,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling((decimal)totalCount / pageSize)
            };

            return Ok(ApiResponse<object>.SuccessResponse(response, "Exams retrieved successfully", 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllExams");
            return StatusCode(500, ApiResponse<List<ExamDto>>.ErrorResponse("An error occurred", "INTERNAL_ERROR", 500));
        }
    }

    /// <summary>
    /// Gets an exam by ID without questions
    /// </summary>
    /// <param name="id">Exam ID</param>
    /// <returns>Exam details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetExamById(Guid id)
    {
        try
        {
            _logger.LogInformation("GetExamById called with ID: {ExamId}", id);

            var exam = await _examService.GetExamByIdAsync(id);
            return Ok(ApiResponse<ExamDto>.SuccessResponse(exam, "Exam retrieved successfully", 200));
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(ApiResponse<ExamDto>.ErrorResponse(ex.Message, ex.ErrorCode, 404));
        }
        catch (ExamAccessDeniedException ex)
        {
            _logger.LogWarning("Access denied: {Message}", ex.Message);
            return BadRequest(ApiResponse<ExamDto>.ErrorResponse(ex.Message, ex.ErrorCode, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExamById");
            return StatusCode(500, ApiResponse<ExamDto>.ErrorResponse("An error occurred", "INTERNAL_ERROR", 500));
        }
    }

    /// <summary>
    /// Gets an exam with all questions (without correct answers)
    /// </summary>
    /// <param name="id">Exam ID</param>
    /// <returns>Exam with questions</returns>
    [HttpGet("{id}/with-questions")]
    public async Task<IActionResult> GetExamWithQuestions(Guid id)
    {
        try
        {
            _logger.LogInformation("GetExamWithQuestions called with ID: {ExamId}", id);

            var exam = await _examService.GetExamWithQuestionsAsync(id);
            return Ok(ApiResponse<ExamResponseDto>.SuccessResponse(exam, "Exam with questions retrieved successfully", 200));
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(ApiResponse<ExamResponseDto>.ErrorResponse(ex.Message, ex.ErrorCode, 404));
        }
        catch (ExamAccessDeniedException ex)
        {
            _logger.LogWarning("Access denied: {Message}", ex.Message);
            return BadRequest(ApiResponse<ExamResponseDto>.ErrorResponse(ex.Message, ex.ErrorCode, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExamWithQuestions");
            return StatusCode(500, ApiResponse<ExamResponseDto>.ErrorResponse("An error occurred", "INTERNAL_ERROR", 500));
        }
    }
}
