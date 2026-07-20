namespace TaO10_BackEnd.DTOs.AiRoadmaps;

public class StudyRoadmapDto
{
    public Guid UserStudyRoadmapId { get; set; }

    public Guid SourceAttemptId { get; set; }

    public DateTime? SourceSubmittedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Summary { get; set; } = string.Empty;

    public List<string> Strengths { get; set; } = new();

    public List<string> Weaknesses { get; set; } = new();

    public List<StudyRoadmapWeekDto> Weeks { get; set; } = new();

    public string DailyTime { get; set; } = string.Empty;

    public string NextAction { get; set; } = string.Empty;
}

public class StudyRoadmapWeekDto
{
    public string Title { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public List<string> Tasks { get; set; } = new();

    public string PracticeType { get; set; } = string.Empty;

    public string PracticeUrl { get; set; } = string.Empty;
}

