using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.DTOs.Questions;
using TaO10_BackEnd.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaO10_BackEnd.Services;

namespace TaO10_BackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private const int MaxPracticeQuestions = 10;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<QuestionsController> _logger;
    private readonly PackageAccessService _packageAccessService;

    public QuestionsController(AppDbContext dbContext, ILogger<QuestionsController> logger, PackageAccessService packageAccessService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _packageAccessService = packageAccessService;
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetQuestionGroups([FromQuery] int sampleSize = 3)
    {
        try
        {
            sampleSize = Math.Clamp(sampleSize, 1, 6);

            var questionEntities = await BuildActiveQuestionsQuery()
                .Where(IsAnswerableQuestionExpression())
                .OrderByDescending(q => q.CreatedAt)
                .ThenBy(q => q.QuestionNumber)
                .ToListAsync();
            var questions = questionEntities
                .Select(q => ToCatalogDto(q, includeAnswer: false))
                .ToList();

            NormalizeQuestionTypes(questions);

            var groups = questions
                .GroupBy(q => q.QuestionType)
                .OrderBy(g => g.Key)
                .Select(g => new QuestionGroupDto
                {
                    Type = g.Key,
                    Count = g.Count(),
                    SampleQuestions = g.Take(sampleSize).ToList()
                })
                .ToList();

            return Ok(ApiResponse<List<QuestionGroupDto>>.SuccessResponse(groups, "Question groups retrieved successfully", 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetQuestionGroups");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred", "INTERNAL_ERROR", 500));
        }
    }

    [Authorize]
    [HttpGet("practice")]
    public async Task<IActionResult> GetPracticeQuestions([FromQuery] string type, [FromQuery] int take = MaxPracticeQuestions)
    {
        try
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdValue, out var userId)) return Unauthorized();
            if (!await _packageAccessService.HasActivePackageAsync(userId, PackageAccessService.PracticeMinimumDays))
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse(
                    "Luyện tập yêu cầu gói từ 3 tháng trở lên.", "PACKAGE_UPGRADE_REQUIRED", 403));
            }
            if (string.IsNullOrWhiteSpace(type))
                return BadRequest(ApiResponse<object>.ErrorResponse("Question type is required", "TYPE_REQUIRED", 400));

            take = Math.Clamp(take, 1, MaxPracticeQuestions);
            var selectedType = QuestionTypeConstants.Normalize(type);

            var questionEntities = await BuildActiveQuestionsQuery()
                .Where(IsAnswerableQuestionExpression())
                .ToListAsync();
            var pool = questionEntities
                .Select(q => ToCatalogDto(q, includeAnswer: true))
                .ToList();

            NormalizeQuestionTypes(pool);

            var matchedPool = pool
                .Where(question => string.Equals(question.QuestionType, selectedType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var questions = matchedPool
                .OrderBy(_ => Random.Shared.Next())
                .Take(take)
                .Select((question, index) =>
                {
                    question.QuestionNumber = index + 1;
                    return question;
                })
                .ToList();

            var response = new
            {
                type = selectedType,
                totalAvailable = matchedPool.Count,
                questions
            };

            return Ok(ApiResponse<object>.SuccessResponse(response, "Practice questions retrieved successfully", 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPracticeQuestions");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred", "INTERNAL_ERROR", 500));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? sortBy = "newest",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Page number and page size must be greater than 0",
                    "INVALID_PAGINATION",
                    400));
            }

            pageSize = Math.Min(pageSize, 100);

            var query = ApplySearch(BuildActiveQuestionsQuery(), search);
            var questionEntities = await query.ToListAsync();
            var questionPool = questionEntities
                .Select(q => ToCatalogDto(q, includeAnswer: false))
                .ToList();

            NormalizeQuestionTypes(questionPool);

            var typeFilters = questionPool
                .GroupBy(q => q.QuestionType)
                .Select(g => new QuestionTypeFilterDto
                {
                    Id = g.Key,
                    Label = g.Key,
                    Count = g.Count()
                })
                .OrderBy(t => t.Label)
                .ToList();

            if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "all", StringComparison.OrdinalIgnoreCase))
            {
                var selectedType = QuestionTypeConstants.Normalize(type);
                questionPool = questionPool
                    .Where(q => string.Equals(q.QuestionType, selectedType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var totalCount = questionPool.Count;
            var questions = ApplySort(questionPool, sortBy)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new
            {
                questions,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling((decimal)totalCount / pageSize),
                typeFilters = new[]
                {
                    new QuestionTypeFilterDto { Id = "all", Label = "Tất cả", Count = typeFilters.Sum(t => t.Count) }
                }.Concat(typeFilters)
            };

            return Ok(ApiResponse<object>.SuccessResponse(response, "Questions retrieved successfully", 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetQuestions");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred", "INTERNAL_ERROR", 500));
        }
    }

    private IQueryable<Question> BuildActiveQuestionsQuery()
    {
        return _dbContext.Questions
            .AsNoTracking()
            .Include(q => q.Status)
            .Include(q => q.Exam)
                .ThenInclude(e => e!.Status)
            .Where(q =>
                q.Status.EntityType == AppStatusCodes.EntityTypes.Question &&
                q.Status.Code == AppStatusCodes.Questions.Active &&
                (q.ExamId == null ||
                    (q.Exam != null &&
                     q.Exam.Status.EntityType == AppStatusCodes.EntityTypes.Exam &&
                     q.Exam.Status.Code == AppStatusCodes.Exams.Active)));
    }

    private static System.Linq.Expressions.Expression<Func<Question, bool>> IsAnswerableQuestionExpression()
    {
        return q =>
            q.OptionA != null && q.OptionA != "" &&
            q.OptionB != null && q.OptionB != "" &&
            q.OptionC != null && q.OptionC != "" &&
            q.OptionD != null && q.OptionD != "" &&
            q.CorrectAnswer != null && q.CorrectAnswer != "";
    }

    private static IQueryable<Question> ApplySearch(IQueryable<Question> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var keyword = search.Trim().ToLower();
        return query.Where(q =>
            q.QuestionText.ToLower().Contains(keyword) ||
            (q.OptionA != null && q.OptionA.ToLower().Contains(keyword)) ||
            (q.OptionB != null && q.OptionB.ToLower().Contains(keyword)) ||
            (q.OptionC != null && q.OptionC.ToLower().Contains(keyword)) ||
            (q.OptionD != null && q.OptionD.ToLower().Contains(keyword)) ||
            (q.Exam != null && q.Exam.Title.ToLower().Contains(keyword)));
    }

    private static IOrderedEnumerable<QuestionCatalogDto> ApplySort(IEnumerable<QuestionCatalogDto> questions, string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "oldest" => questions.OrderBy(q => q.CreatedAt).ThenBy(q => q.QuestionNumber),
            "number-asc" => questions.OrderBy(q => q.ExamTitle ?? string.Empty).ThenBy(q => q.QuestionNumber),
            "number-desc" => questions.OrderBy(q => q.ExamTitle ?? string.Empty).ThenByDescending(q => q.QuestionNumber),
            "type-asc" => questions.OrderBy(q => q.QuestionType).ThenByDescending(q => q.CreatedAt),
            _ => questions.OrderByDescending(q => q.CreatedAt).ThenBy(q => q.QuestionNumber)
        };
    }

    private static QuestionCatalogDto ToCatalogDto(Question question, bool includeAnswer)
    {
        var isPassage =
            question.QuestionNumber == 0 &&
            question.OptionA == null &&
            question.OptionB == null &&
            question.OptionC == null &&
            question.OptionD == null &&
            question.CorrectAnswer == null;

        return new QuestionCatalogDto
        {
            QuestionId = question.QuestionId,
            ExamId = question.ExamId,
            ExamTitle = question.Exam != null ? question.Exam.Title : null,
            QuestionNumber = isPassage ? null : question.QuestionNumber,
            QuestionType = question.Section ?? QuestionTypeConstants.Other,
            Section = question.Section,
            QuestionText = question.QuestionText,
            OptionA = question.OptionA,
            OptionB = question.OptionB,
            OptionC = question.OptionC,
            OptionD = question.OptionD,
            CorrectAnswer = includeAnswer ? question.CorrectAnswer : null,
            Explanation = includeAnswer ? question.Explanation : null,
            Points = question.Points,
            Level = question.Exam != null ? question.Exam.Level : null,
            Year = question.Exam != null ? question.Exam.Year : null,
            ExamType = question.Exam != null ? question.Exam.ExamType : null,
            CreatedAt = question.CreatedAt
        };
    }

    private static void NormalizeQuestionTypes(IEnumerable<QuestionCatalogDto> questions)
    {
        foreach (var question in questions)
        {
            var normalized = QuestionTypeConstants.Normalize(question.Section, question.QuestionNumber);
            question.QuestionType = normalized;
            question.Section = normalized;
        }
    }
}
