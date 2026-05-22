using MiniSlack.Domain.Common;
using MiniSlack.Domain.Users;

namespace MiniSlack.Domain.Workspaces;

public sealed class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; set; }

    public Workspace? Workspace { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public WorkspaceMemberRole Role { get; set; }

    public DateTimeOffset JoinedAtUtc { get; set; }
}
