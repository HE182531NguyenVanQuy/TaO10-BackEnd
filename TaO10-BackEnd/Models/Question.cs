using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class Question
{
    public Guid QuestionId { get; set; }

    public Guid ExamId { get; set; }

    public int QuestionNumber { get; set; }

    public string? Section { get; set; }

    public string QuestionText { get; set; } = null!;

    public string? OptionA { get; set; }

    public string? OptionB { get; set; }

    public string? OptionC { get; set; }

    public string? OptionD { get; set; }

    public string? CorrectAnswer { get; set; }

    public string? Explanation { get; set; }

    public decimal? Points { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
