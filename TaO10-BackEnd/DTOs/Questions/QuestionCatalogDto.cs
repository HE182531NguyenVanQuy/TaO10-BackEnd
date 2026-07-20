namespace TaO10_BackEnd.DTOs.Questions;

public class QuestionCatalogDto
{
    public Guid QuestionId { get; set; }
    public Guid? ExamId { get; set; }
    public string? ExamTitle { get; set; }
    public int? QuestionNumber { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? OptionA { get; set; }
    public string? OptionB { get; set; }
    public string? OptionC { get; set; }
    public string? OptionD { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public decimal? Points { get; set; }
    public string? Level { get; set; }
    public int? Year { get; set; }
    public string? ExamType { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class QuestionTypeFilterDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class QuestionGroupDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<QuestionCatalogDto> SampleQuestions { get; set; } = [];
}
