using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class PasswordResetToken
{
    public Guid ResetTokenId { get; set; }

    public Guid UserId { get; set; }

    public string OtpCode { get; set; } = null!;

    public DateTime ExpiryTime { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
