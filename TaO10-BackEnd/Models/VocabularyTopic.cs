using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class VocabularyTopic
{
    public Guid VocabularyTopicId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? WordsCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<VocabularyWord> VocabularyWords { get; set; } = new List<VocabularyWord>();
}
