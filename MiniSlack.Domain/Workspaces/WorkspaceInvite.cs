using MiniSlack.Domain.Common;
using MiniSlack.Domain.Users;

namespace MiniSlack.Domain.Workspaces;

public sealed class WorkspaceInvite : BaseEntity
{
    public Guid WorkspaceId { get; set; }

    public Workspace? Workspace { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public WorkspaceMemberRole Role { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public Guid InvitedByUserId { get; set; }

    public User? InvitedByUser { get; set; }

    public Guid? AcceptedByUserId { get; set; }

    public User? AcceptedByUser { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? AcceptedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }
}
