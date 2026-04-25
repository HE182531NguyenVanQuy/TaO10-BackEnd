using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class GrammarLesson
{
    public Guid GrammarLessonId { get; set; }

    public Guid GrammarTopicId { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public string? Level { get; set; }

    public string? LessonType { get; set; }

    public int? OrderIndex { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual GrammarTopic GrammarTopic { get; set; } = null!;
}
