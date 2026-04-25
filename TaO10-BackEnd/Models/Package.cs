using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class Package
{
    public Guid PackageId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int Price { get; set; }

    public int ExamLimit { get; set; }

    public int? DurationDays { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();
}
