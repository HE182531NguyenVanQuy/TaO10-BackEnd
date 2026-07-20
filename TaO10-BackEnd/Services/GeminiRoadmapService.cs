using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.Exceptions;
using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Services;

public class OpenRouterRoadmapService : IOpenRouterRoadmapService
{
    private const int MaxAttempts = 2;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterRoadmapService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public OpenRouterRoadmapService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenRouterRoadmapService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GeneratedRoadmap> GenerateRoadmapAsync(UserExamAttempt attempt, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new OpenRouterUnavailableException("Chưa cấu hình OpenRouter API key trên backend.");
        }

        var prompt = BuildPrompt(attempt);
        var model = _configuration["OpenRouter:Model"]?.Trim();
        return await GenerateWithModelAsync(
            string.IsNullOrWhiteSpace(model) ? "openrouter/free" : model,
            apiKey,
            prompt,
            cancellationToken);
    }

    private async Task<GeneratedRoadmap> GenerateWithModelAsync(
        string model,
        string apiKey,
        string prompt,
        CancellationToken cancellationToken)
    {
        var requestBody = BuildRequestBody(prompt);

        for (var attemptNumber = 1; attemptNumber <= MaxAttempts; attemptNumber++)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Headers.TryAddWithoutValidation("HTTP-Referer", _configuration["OpenRouter:SiteUrl"] ?? "https://tao10m.com");
                request.Headers.TryAddWithoutValidation("X-Title", _configuration["OpenRouter:AppName"] ?? "TaO10");
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "OpenRouter request completed. Model: {Model}. Attempt: {AttemptNumber}/{MaxAttempts}. PromptLength: {PromptLength}. StatusCode: {StatusCode}. DurationMs: {DurationMs}",
                    model,
                    attemptNumber,
                    MaxAttempts,
                    prompt.Length,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                if (response.IsSuccessStatusCode)
                {
                    return ParseOpenRouterRoadmapResponse(responseBody);
                }

                if (!ShouldRetry(response.StatusCode) || attemptNumber == MaxAttempts)
                {
                    ThrowOpenRouterException(response.StatusCode);
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    ex,
                    "OpenRouter request timed out. Model: {Model}. Attempt: {AttemptNumber}/{MaxAttempts}. PromptLength: {PromptLength}. DurationMs: {DurationMs}",
                    model,
                    attemptNumber,
                    MaxAttempts,
                    prompt.Length,
                    stopwatch.ElapsedMilliseconds);

                if (attemptNumber == MaxAttempts)
                {
                    throw new OpenRouterUnavailableException();
                }
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    ex,
                    "OpenRouter request failed. Model: {Model}. Attempt: {AttemptNumber}/{MaxAttempts}. PromptLength: {PromptLength}. DurationMs: {DurationMs}",
                    model,
                    attemptNumber,
                    MaxAttempts,
                    prompt.Length,
                    stopwatch.ElapsedMilliseconds);

                if (attemptNumber == MaxAttempts)
                {
                    throw new OpenRouterUnavailableException();
                }
            }

            var delay = GetRetryDelay(attemptNumber);
            _logger.LogInformation(
                "Retrying OpenRouter request after {DelayMs}ms. Model: {Model}. NextAttempt: {NextAttempt}/{MaxAttempts}",
                delay.TotalMilliseconds,
                model,
                attemptNumber + 1,
                MaxAttempts);
            await Task.Delay(delay, cancellationToken);
        }

        throw new OpenRouterUnavailableException();
    }

    private string BuildRequestBody(string prompt)
    {
        var request = new
        {
            model = _configuration["OpenRouter:Model"] ?? "openrouter/free",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.25,
            response_format = new { type = "json_object" }
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

    private GeneratedRoadmap ParseOpenRouterRoadmapResponse(string responseBody)
    {
        var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseBody, _jsonOptions);
        var text = openRouterResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new OpenRouterUnavailableException("OpenRouter không trả về nội dung lộ trình.");
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
            throw new OpenRouterUnavailableException("Không đọc được JSON lộ trình từ OpenRouter.");
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
            throw new OpenRouterUnavailableException("OpenRouter trả về lộ trình thiếu dữ liệu.");
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

    private static void ThrowOpenRouterException(HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
        {
            throw new OpenRouterUnavailableException("OpenRouter API key không hợp lệ hoặc chưa được cấp quyền.");
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            throw new OpenRouterQuotaExceededException();
        }

        if (statusCode == HttpStatusCode.ServiceUnavailable ||
            statusCode == HttpStatusCode.InternalServerError ||
            statusCode == HttpStatusCode.RequestTimeout)
        {
            throw new OpenRouterUnavailableException();
        }

        throw new OpenRouterUnavailableException("Không gọi được OpenRouter để tạo lộ trình.");
    }

    private sealed record SectionQuestionResult(string Section, bool IsCorrect, bool IsSkipped);

    private sealed class OpenRouterResponse
    {
        public List<OpenRouterChoice>? Choices { get; set; }
    }

    private sealed class OpenRouterChoice
    {
        public OpenRouterMessage? Message { get; set; }
    }

    private sealed class OpenRouterMessage
    {
        public string? Content { get; set; }
    }
}
