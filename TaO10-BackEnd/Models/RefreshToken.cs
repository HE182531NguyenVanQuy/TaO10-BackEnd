using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class RefreshToken
{
    public Guid RefreshTokenId { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool? IsRevoked { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
