using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class Notification
{
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
