using MiniSlack.Domain.Common;
using MiniSlack.Domain.Users;

namespace MiniSlack.Domain.Auth;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }
}
