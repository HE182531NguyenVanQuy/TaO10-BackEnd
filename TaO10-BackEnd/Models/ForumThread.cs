using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class ForumThread
{
    public Guid ForumThreadId { get; set; }

    public Guid UserId { get; set; }

    public Guid? ForumCategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Excerpt { get; set; }

    public bool? IsPinned { get; set; }

    public bool? IsHot { get; set; }

    public string? Tags { get; set; }

    public int? ViewsCount { get; set; }

    public int? RepliesCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ForumCategory? ForumCategory { get; set; }

    public virtual ICollection<ForumReply> ForumReplies { get; set; } = new List<ForumReply>();

    public virtual User User { get; set; } = null!;
}
