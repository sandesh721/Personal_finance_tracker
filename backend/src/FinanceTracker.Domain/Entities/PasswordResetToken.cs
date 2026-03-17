using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public sealed class PasswordResetToken : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
    public DateTime? UsedUtc { get; set; }
    public string? RequestedIpAddress { get; set; }
    public string? RequestedUserAgent { get; set; }

    public User User { get; set; } = null!;

    public bool IsActive => UsedUtc is null && ExpiresUtc > DateTime.UtcNow;
}
