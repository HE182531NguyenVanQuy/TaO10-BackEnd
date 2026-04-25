using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string Phone { get; set; } = null!;

    public string? Location { get; set; }

    public int? TotalScore { get; set; }

    public int? TotalExams { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsActive { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<ForumReply> ForumReplies { get; set; } = new List<ForumReply>();

    public virtual ICollection<ForumThread> ForumThreads { get; set; } = new List<ForumThread>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<UserExamAttempt> UserExamAttempts { get; set; } = new List<UserExamAttempt>();

    public virtual ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();

    public virtual ICollection<UserProgress> UserProgresses { get; set; } = new List<UserProgress>();
}
