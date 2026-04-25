using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class Exam
{
    public Guid ExamId { get; set; }

    public Guid? PackageId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int QuestionsCount { get; set; }

    public int DurationMinutes { get; set; }

    public string? Difficulty { get; set; }

    public int? Year { get; set; }

    public string? ExamType { get; set; }

    public int? ViewsCount { get; set; }

    public int? AttemptsCount { get; set; }

    public string? CoverClass { get; set; }

    public string? Tag { get; set; }

    public string? TagClass { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual Package? Package { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<UserExamAttempt> UserExamAttempts { get; set; } = new List<UserExamAttempt>();
}
