using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Application.Workspaces;

public sealed record WorkspaceInviteSummary(
    Guid Id,
    Guid WorkspaceId,
    string Email,
    WorkspaceMemberRole Role,
    Guid InvitedByUserId,
    string InvitedByDisplayName,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    DateTimeOffset CreatedAtUtc);
