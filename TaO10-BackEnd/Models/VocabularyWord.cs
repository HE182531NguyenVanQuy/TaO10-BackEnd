using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class VocabularyWord
{
    public Guid VocabularyWordId { get; set; }

    public Guid VocabularyTopicId { get; set; }

    public string Word { get; set; } = null!;

    public string? Phonetic { get; set; }

    public string Meaning { get; set; } = null!;

    public string? WordType { get; set; }

    public string? Example { get; set; }

    public string? ExampleTranslation { get; set; }

    public string? Difficulty { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual VocabularyTopic VocabularyTopic { get; set; } = null!;
}
