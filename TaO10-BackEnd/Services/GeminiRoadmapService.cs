using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.Exceptions;
using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Services;

public class GeminiRoadmapService : IGeminiRoadmapService
{
    private const int MaxAttemptsPerModel = 5;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiRoadmapService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public GeminiRoadmapService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiRoadmapService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GeneratedRoadmap> GenerateRoadmapAsync(UserExamAttempt attempt, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new GeminiUnavailableException("Chưa cấu hình Gemini API key trên backend.");
        }

        var prompt = BuildPrompt(attempt);
        var models = GetConfiguredModels();

        for (var modelIndex = 0; modelIndex < models.Count; modelIndex++)
        {
            var model = models[modelIndex];
            try
            {
                return await GenerateWithModelAsync(model, apiKey, prompt, cancellationToken);
            }
            catch (GeminiUnavailableException) when (modelIndex < models.Count - 1)
            {
                _logger.LogWarning(
                    "Gemini model {Model} remained unavailable after {MaxAttempts} attempts. Falling back to {FallbackModel}.",
                    model,
                    MaxAttemptsPerModel,
                    models[modelIndex + 1]);
            }
        }

        throw new GeminiUnavailableException();
    }

    private async Task<GeneratedRoadmap> GenerateWithModelAsync(
        string model,
        string apiKey,
        string prompt,
        CancellationToken cancellationToken)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        var requestBody = BuildRequestBody(prompt);

        for (var attemptNumber = 1; attemptNumber <= MaxAttemptsPerModel; attemptNumber++)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Gemini request completed. Model: {Model}. Attempt: {AttemptNumber}/{MaxAttempts}. PromptLength: {PromptLength}. StatusCode: {StatusCode}. DurationMs: {DurationMs}. ResponseBody: {ResponseBody}",
                    model,
                    attemptNumber,
                    MaxAttemptsPerModel,
                    prompt.Length,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return ParseGeminiRoadmapResponse(responseBody);
                }

                if (!ShouldRetry(response.StatusCode) || attemptNumber == MaxAttemptsPerModel)
                {
                    ThrowGeminiException(response.StatusCode);
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    ex,
                    "Gemini request timed out. Model: {Model}. Attempt: {AttemptNumber}/{MaxAttempts}. PromptLength: {PromptLength}. DurationMs: {DurationMs}",
                    model,
                    attemptNumber,
                    MaxAttemptsPerModel,
                    prompt.Length,
                    stopwatch.ElapsedMilliseconds);

                if (attemptNumber == MaxAttemptsPerModel)
                {
                    throw new GeminiUnavailableException();
                }
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    ex,
                    "Gemini request failed. Model: {Model}. Attempt: {AttemptNumber}/{MaxAttempts}. PromptLength: {PromptLength}. DurationMs: {DurationMs}",
                    model,
                    attemptNumber,
                    MaxAttemptsPerModel,
                    prompt.Length,
                    stopwatch.ElapsedMilliseconds);

                if (attemptNumber == MaxAttemptsPerModel)
                {
                    throw new GeminiUnavailableException();
                }
            }

            var delay = GetRetryDelay(attemptNumber);
            _logger.LogInformation(
                "Retrying Gemini request after {DelayMs}ms. Model: {Model}. NextAttempt: {NextAttempt}/{MaxAttempts}",
                delay.TotalMilliseconds,
                model,
                attemptNumber + 1,
                MaxAttemptsPerModel);
            await Task.Delay(delay, cancellationToken);
        }

        throw new GeminiUnavailableException();
    }

    private List<string> GetConfiguredModels()
    {
        var models = new[] { _configuration["Gemini:Model"], _configuration["Gemini:FallbackModel"] }
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return models.Count > 0 ? models : new List<string> { "gemini-2.5-flash" };
    }

    private string BuildRequestBody(string prompt)
    {
        var request = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.25,
                responseMimeType = "application/json"
            }
        };

        return JsonSerializer.Serialize(request, _jsonOptions);
    }

    private string BuildPrompt(UserExamAttempt attempt)
    {
        var totalQuestions = attempt.TotalQuestions ?? attempt.Exam?.QuestionsCount ?? attempt.Exam?.Questions.Count ?? 0;
        var answeredQuestionIds = attempt.UserAnswers
            .Where(answer => answer.QuestionId.HasValue)
            .Select(answer => answer.QuestionId!.Value)
            .ToHashSet();

        var answeredBySection = attempt.UserAnswers
            .Where(answer => answer.Question != null)
            .Select(answer => new SectionQuestionResult(
                NormalizeSection(answer.Question!.Section),
                answer.IsCorrect == true,
                false));

        var skippedBySection = (attempt.Exam?.Questions ?? new List<Question>())
            .Where(question => !answeredQuestionIds.Contains(question.QuestionId))
            .Select(question => new SectionQuestionResult(
                NormalizeSection(question.Section),
                false,
                true));

        var sectionStats = answeredBySection
            .Concat(skippedBySection)
            .GroupBy(item => item.Section)
            .Select(group => new
            {
                section = group.Key,
                correct = group.Count(item => item.IsCorrect),
                wrong = group.Count(item => !item.IsCorrect && !item.IsSkipped),
                skipped = group.Count(item => item.IsSkipped),
                total = group.Count()
            })
            .OrderByDescending(item => item.wrong + item.skipped)
            .ThenBy(item => item.section)
            .ToList();

        var correctAnswers = attempt.CorrectAnswers ?? sectionStats.Sum(item => item.correct);
        var skipped = sectionStats.Sum(item => item.skipped);
        var wrong = Math.Max(0, totalQuestions - correctAnswers - skipped);
        var score = attempt.Score.HasValue ? Math.Round(attempt.Score.Value / 10, 2) : 0;

        var attemptSummary = new
        {
            examTitle = attempt.Exam?.Title,
            completedAt = attempt.CompletedAt,
            score,
            totalQuestions,
            correct = correctAnswers,
            wrong,
            skipped,
            sections = sectionStats
        };

        return $$"""
Bạn là giáo viên tiếng Anh luyện thi vào lớp 10. Tạo lộ trình học cá nhân hóa 3 tuần từ thống kê bài làm.
Chỉ trả về JSON hợp lệ, không markdown, không giải thích.
Schema:
{"summary":"string","strengths":["string"],"weaknesses":["string"],"weeks":[{"title":"string","goal":"string","tasks":["string"],"practiceType":"string"}],"dailyTime":"string","nextAction":"string"}
practiceType phải là một trong các type sau: {{string.Join(", ", QuestionTypeConstants.All)}}.
Mỗi tuần chọn đúng 1 practiceType ứng với điểm yếu cần luyện nhiều nhất tuần đó, ưu tiên section sai hoặc bỏ qua nhiều.
Dữ liệu:
{{JsonSerializer.Serialize(attemptSummary, _jsonOptions)}}
""";
    }

    private GeneratedRoadmap ParseGeminiRoadmapResponse(string responseBody)
    {
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, _jsonOptions);
        var text = string.Join(
            string.Empty,
            geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.Select(part => part.Text ?? string.Empty) ?? Array.Empty<string>());

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new GeminiUnavailableException("Gemini không trả về nội dung lộ trình.");
        }

        return ParseGeneratedRoadmap(text);
    }

    private GeneratedRoadmap ParseGeneratedRoadmap(string text)
    {
        var cleaned = text.Trim();
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..].Trim();
        }
        else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..].Trim();
        }

        if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^3].Trim();
        }

        var roadmap = JsonSerializer.Deserialize<GeneratedRoadmap>(cleaned, _jsonOptions);
        if (roadmap == null)
        {
            throw new GeminiUnavailableException("Không đọc được JSON lộ trình từ Gemini.");
        }

        ValidateGeneratedRoadmap(roadmap);

        roadmap.Strengths = roadmap.Strengths.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
        roadmap.Weaknesses = roadmap.Weaknesses.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
        roadmap.Weeks = roadmap.Weeks.Select(week => new StudyRoadmapWeekResult
        {
            Title = week.Title,
            Goal = week.Goal,
            Tasks = week.Tasks.Where(item => !string.IsNullOrWhiteSpace(item)).ToList(),
            PracticeType = QuestionTypeConstants.Normalize(week.PracticeType)
        }).ToList();

        return roadmap;
    }

    private static void ValidateGeneratedRoadmap(GeneratedRoadmap roadmap)
    {
        if (string.IsNullOrWhiteSpace(roadmap.Summary) ||
            roadmap.Strengths.Count == 0 ||
            roadmap.Weaknesses.Count == 0 ||
            roadmap.Weeks.Count == 0 ||
            roadmap.Weeks.Any(week =>
                string.IsNullOrWhiteSpace(week.Title) ||
                string.IsNullOrWhiteSpace(week.Goal) ||
                week.Tasks.Count == 0 ||
                week.Tasks.Any(string.IsNullOrWhiteSpace)) ||
            string.IsNullOrWhiteSpace(roadmap.DailyTime) ||
            string.IsNullOrWhiteSpace(roadmap.NextAction))
        {
            throw new GeminiUnavailableException("Gemini trả về lộ trình thiếu dữ liệu.");
        }
    }

    private static string NormalizeSection(string? section)
    {
        return QuestionTypeConstants.Normalize(section);
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests ||
            statusCode == HttpStatusCode.ServiceUnavailable ||
            statusCode == HttpStatusCode.InternalServerError ||
            statusCode == HttpStatusCode.RequestTimeout;
    }

    private static TimeSpan GetRetryDelay(int attemptNumber)
    {
        var exponentialSeconds = Math.Pow(2, attemptNumber - 1);
        var jitterMs = Random.Shared.Next(150, 900);
        return TimeSpan.FromSeconds(exponentialSeconds) + TimeSpan.FromMilliseconds(jitterMs);
    }

    private static void ThrowGeminiException(HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
        {
            throw new GeminiUnavailableException("Gemini API key khong hop le hoac chua duoc cap quyen.");
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            throw new GeminiQuotaExceededException();
        }

        if (statusCode == HttpStatusCode.ServiceUnavailable ||
            statusCode == HttpStatusCode.InternalServerError ||
            statusCode == HttpStatusCode.RequestTimeout)
        {
            throw new GeminiUnavailableException();
        }

        throw new GeminiUnavailableException("Không gọi được Gemini để tạo lộ trình.");
    }

    private sealed record SectionQuestionResult(string Section, bool IsCorrect, bool IsSkipped);

    private sealed class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        public string? Text { get; set; }
    }
}
