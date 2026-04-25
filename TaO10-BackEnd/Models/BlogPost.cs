using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class BlogPost
{
    public Guid BlogPostId { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public string? Meta { get; set; }

    public string? ThumbClass { get; set; }

    public string? ReadTime { get; set; }

    public int? ViewsCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public bool? IsPublished { get; set; }
}
