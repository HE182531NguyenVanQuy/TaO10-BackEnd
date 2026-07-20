using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.DTOs.AiRoadmaps;
using TaO10_BackEnd.Exceptions;
using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Services;

public class AiRoadmapService : IAiRoadmapService
{
    private readonly AppDbContext _dbContext;
    private readonly IOpenRouterRoadmapService _openRouterRoadmapService;
    private readonly ILogger<AiRoadmapService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyList<string> DefaultPracticeTypeOrder =
    [
        QuestionTypeConstants.MultipleChoice,
        QuestionTypeConstants.Reading,
        QuestionTypeConstants.Cloze,
        QuestionTypeConstants.ErrorCorrection,
        QuestionTypeConstants.Pronunciation,
        QuestionTypeConstants.Stress,
        QuestionTypeConstants.Communication,
        QuestionTypeConstants.Synonym,
        QuestionTypeConstants.Antonym,
        QuestionTypeConstants.Rewrite,
        QuestionTypeConstants.RewriteWithGivenWords,
        QuestionTypeConstants.SentenceInsertion,
        QuestionTypeConstants.SignNotice,
        QuestionTypeConstants.Vocabulary,
        QuestionTypeConstants.ArrangeCompleteParagraph
    ];

    public AiRoadmapService(
        AppDbContext dbContext,
        IOpenRouterRoadmapService openRouterRoadmapService,
        ILogger<AiRoadmapService> logger)
    {
        _dbContext = dbContext;
        _openRouterRoadmapService = openRouterRoadmapService;
        _logger = logger;
    }

    public async Task<StudyRoadmapDto?> GetRoadmapAsync(Guid userId)
    {
        var roadmap = await GetExistingRoadmapAsync(userId);
        return roadmap == null ? null : MapToDto(roadmap);
    }

    public async Task<StudyRoadmapDto> GenerateRoadmapAsync(Guid userId)
    {
        var attempt = await GetLatestCompletedAttemptAsync(userId);
        if (attempt == null || attempt.UserAnswers.Count == 0)
        {
            throw new InvalidOperationException("Báº¡n cáº§n lÃ m bÃ i Ã­t nháº¥t 1 láº§n Ä‘á»ƒ cÃ³ dá»¯ liá»‡u phÃ¢n tÃ­ch");
        }

        var existing = await GetExistingRoadmapAsync(userId);
        if (existing?.UserExamAttemptId == attempt.UserExamAttemptId)
        {
            return MapToDto(existing);
        }

        var generated = await GenerateWithFallbackAsync(attempt);
        var roadmap = existing ?? CreateRoadmap(userId);
        ApplyGeneratedRoadmap(roadmap, attempt, generated);

        await _dbContext.SaveChangesAsync();

        roadmap.UserExamAttempt = attempt;
        return MapToDto(roadmap);
    }

    private async Task<GeneratedRoadmap> GenerateWithFallbackAsync(UserExamAttempt attempt)
    {
        try
        {
            return await _openRouterRoadmapService.GenerateRoadmapAsync(attempt);
        }
        catch (Exception ex) when (ex is OpenRouterUnavailableException or OpenRouterQuotaExceededException or JsonException)
        {
            _logger.LogWarning(ex, "OpenRouter roadmap failed. Using local fallback roadmap JSON.");
            return AiRoadmapFallbackLibrary.GetRandomRoadmap();
        }
    }

    private async Task<UserStudyRoadmap?> GetExistingRoadmapAsync(Guid userId)
    {
        return await _dbContext.UserStudyRoadmaps
            .Include(item => item.UserExamAttempt)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<UserExamAttempt?> GetLatestCompletedAttemptAsync(Guid userId)
    {
        return await _dbContext.UserExamAttempts
            .Include(attempt => attempt.Exam)
                .ThenInclude(exam => exam!.Questions)
            .Include(attempt => attempt.Status)
            .Include(attempt => attempt.UserAnswers)
                .ThenInclude(answer => answer.Question)
            .Where(attempt =>
                attempt.UserId == userId &&
                attempt.CompletedAt != null &&
                attempt.Status.Code == AppStatusCodes.Attempts.Submitted)
            .OrderByDescending(attempt => attempt.CompletedAt)
            .FirstOrDefaultAsync();
    }

    private UserStudyRoadmap CreateRoadmap(Guid userId)
    {
        var roadmap = new UserStudyRoadmap
        {
            UserStudyRoadmapId = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserStudyRoadmaps.Add(roadmap);
        return roadmap;
    }

    private void ApplyGeneratedRoadmap(
        UserStudyRoadmap roadmap,
        UserExamAttempt attempt,
        GeneratedRoadmap generated)
    {
        roadmap.UserExamAttemptId = attempt.UserExamAttemptId;
        roadmap.Summary = generated.Summary;
        roadmap.Strengths = JsonSerializer.Serialize(generated.Strengths, _jsonOptions);
        roadmap.Weaknesses = JsonSerializer.Serialize(generated.Weaknesses, _jsonOptions);
        roadmap.Weeks = JsonSerializer.Serialize(
            EnrichPracticeTargets(generated.Weeks.Select(week => new StudyRoadmapWeekDto
            {
                Title = week.Title,
                Goal = week.Goal,
                Tasks = week.Tasks,
                PracticeType = week.PracticeType
            }).ToList(), generated.Weaknesses),
            _jsonOptions);
        roadmap.DailyTime = generated.DailyTime;
        roadmap.NextAction = generated.NextAction;
        roadmap.UpdatedAt = DateTime.UtcNow;
    }

    private StudyRoadmapDto MapToDto(UserStudyRoadmap roadmap)
    {
        return new StudyRoadmapDto
        {
            UserStudyRoadmapId = roadmap.UserStudyRoadmapId,
            SourceAttemptId = roadmap.UserExamAttemptId,
            SourceSubmittedAt = roadmap.UserExamAttempt?.CompletedAt,
            CreatedAt = roadmap.CreatedAt,
            Summary = roadmap.Summary,
            Strengths = DeserializeList(roadmap.Strengths),
            Weaknesses = DeserializeList(roadmap.Weaknesses),
            Weeks = EnrichPracticeTargets(DeserializeWeeks(roadmap.Weeks), DeserializeList(roadmap.Weaknesses)),
            DailyTime = roadmap.DailyTime,
            NextAction = roadmap.NextAction
        };
    }

    private List<string> DeserializeList(string value)
    {
        return JsonSerializer.Deserialize<List<string>>(value, _jsonOptions) ?? new List<string>();
    }

    private List<StudyRoadmapWeekDto> DeserializeWeeks(string value)
    {
        return JsonSerializer.Deserialize<List<StudyRoadmapWeekDto>>(value, _jsonOptions) ?? new List<StudyRoadmapWeekDto>();
    }

    private static List<StudyRoadmapWeekDto> EnrichPracticeTargets(
        List<StudyRoadmapWeekDto> weeks,
        List<string> weaknesses)
    {
        var fallbackWeaknessText = string.Join(' ', weaknesses);
        var usedPracticeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < weeks.Count; index++)
        {
            var week = weeks[index];
            var candidateText = string.Join(
                ' ',
                new[]
                {
                    week.PracticeType,
                    week.Title,
                    week.Goal,
                    string.Join(' ', week.Tasks),
                    fallbackWeaknessText
                });

            var practiceType = EnsureUniquePracticeType(InferPracticeType(candidateText), usedPracticeTypes, candidateText);
            week.PracticeType = practiceType;
            week.PracticeUrl = $"/luyen-tap?type={Uri.EscapeDataString(practiceType)}&week={index + 1}";
        }

        return weeks;
    }

    private static string EnsureUniquePracticeType(
        string preferredType,
        HashSet<string> usedPracticeTypes,
        string contextText)
    {
        var normalizedPreferred = QuestionTypeConstants.Normalize(preferredType);
        if (IsUsablePracticeType(normalizedPreferred) && usedPracticeTypes.Add(normalizedPreferred))
            return normalizedPreferred;

        foreach (var candidate in GetCandidatePracticeTypes(contextText))
        {
            var normalized = QuestionTypeConstants.Normalize(candidate);
            if (IsUsablePracticeType(normalized) && usedPracticeTypes.Add(normalized))
                return normalized;
        }

        foreach (var fallbackType in DefaultPracticeTypeOrder)
        {
            if (usedPracticeTypes.Add(fallbackType))
                return fallbackType;
        }

        return normalizedPreferred;
    }

    private static IEnumerable<string> GetCandidatePracticeTypes(string text)
    {
        var lowerText = RemoveDiacritics(text).ToLowerInvariant();

        foreach (var type in QuestionTypeConstants.All)
        {
            if (!IsUsablePracticeType(type))
                continue;

            var key = RemoveDiacritics(type).ToLowerInvariant();
            if (lowerText.Contains(key))
                yield return type;
        }
    }

    private static bool IsUsablePracticeType(string? type)
    {
        return !string.IsNullOrWhiteSpace(type) &&
            !string.Equals(type, QuestionTypeConstants.Other, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(type, QuestionTypeConstants.Mixed, StringComparison.OrdinalIgnoreCase);
    }

    private static string InferPracticeType(string text)
    {
        var normalized = QuestionTypeConstants.Normalize(text);
        if (!string.Equals(normalized, QuestionTypeConstants.Other, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, text?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        var lowerText = RemoveDiacritics(text).ToLowerInvariant();
        foreach (var type in QuestionTypeConstants.All)
        {
            if (type == QuestionTypeConstants.Other)
                continue;

            var key = RemoveDiacritics(type).ToLowerInvariant();
            if (lowerText.Contains(key))
                return type;
        }

        return QuestionTypeConstants.MultipleChoice;
    }

    private static string RemoveDiacritics(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(ch switch
            {
                '\u0111' or '\u0110' => 'd',
                _ => ch
            });
        }

        return builder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}

