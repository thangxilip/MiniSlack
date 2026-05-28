using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Application.Workspaces;

public sealed record CreateWorkspaceInviteRequest(
    string Email,
    WorkspaceMemberRole Role);
