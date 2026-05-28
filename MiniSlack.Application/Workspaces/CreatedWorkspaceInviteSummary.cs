using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Application.Workspaces;

public sealed record CreatedWorkspaceInviteSummary(
    Guid Id,
    Guid WorkspaceId,
    string Email,
    WorkspaceMemberRole Role,
    Guid InvitedByUserId,
    string InvitedByDisplayName,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset CreatedAtUtc,
    string Token,
    string AcceptUrl);
