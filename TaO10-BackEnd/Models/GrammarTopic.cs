using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class GrammarTopic
{
    public Guid GrammarTopicId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? LessonsCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<GrammarLesson> GrammarLessons { get; set; } = new List<GrammarLesson>();
}
