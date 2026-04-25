using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class UserProgress
{
    public Guid ProgressId { get; set; }

    public Guid UserId { get; set; }

    public string ItemType { get; set; } = null!;

    public Guid ItemId { get; set; }

    public int? ProgressPercentage { get; set; }

    public DateTime? LastAccessed { get; set; }

    public virtual User User { get; set; } = null!;
}
