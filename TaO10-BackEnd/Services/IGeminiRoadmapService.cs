using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Services;

public interface IOpenRouterRoadmapService
{
    Task<GeneratedRoadmap> GenerateRoadmapAsync(UserExamAttempt attempt, CancellationToken cancellationToken = default);
}

public sealed class GeneratedRoadmap
{
    public string Summary { get; set; } = string.Empty;

    public List<string> Strengths { get; set; } = new();

    public List<string> Weaknesses { get; set; } = new();

    public List<StudyRoadmapWeekResult> Weeks { get; set; } = new();

    public string DailyTime { get; set; } = string.Empty;

    public string NextAction { get; set; } = string.Empty;
}

public sealed class StudyRoadmapWeekResult
{
    public string Title { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public List<string> Tasks { get; set; } = new();

    public string PracticeType { get; set; } = string.Empty;
}
