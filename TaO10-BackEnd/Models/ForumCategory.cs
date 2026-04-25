using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class ForumCategory
{
    public Guid ForumCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? ThreadsCount { get; set; }

    public int? RepliesCount { get; set; }

    public string? Badge { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ForumThread> ForumThreads { get; set; } = new List<ForumThread>();
}
