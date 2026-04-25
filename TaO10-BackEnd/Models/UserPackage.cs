using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class UserPackage
{
    public Guid UserPackageId { get; set; }

    public Guid UserId { get; set; }

    public Guid PackageId { get; set; }

    public Guid? PaymentId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int RemainingExams { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Package Package { get; set; } = null!;

    public virtual Payment? Payment { get; set; }

    public virtual User User { get; set; } = null!;
}
