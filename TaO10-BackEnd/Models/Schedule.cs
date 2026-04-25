using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class Schedule
{
    public Guid ScheduleId { get; set; }

    public string Title { get; set; } = null!;

    public int? Day { get; set; }

    public string? Month { get; set; }

    public string? Time { get; set; }

    public string? BgClass { get; set; }

    public DateOnly? EventDate { get; set; }

    public DateTime? CreatedAt { get; set; }
}
