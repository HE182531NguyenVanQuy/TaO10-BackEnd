using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class ForumReply
{
    public Guid ForumReplyId { get; set; }

    public Guid ForumThreadId { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ForumThread ForumThread { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
