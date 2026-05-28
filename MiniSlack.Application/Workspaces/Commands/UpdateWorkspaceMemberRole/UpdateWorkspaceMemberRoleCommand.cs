using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.UpdateWorkspaceMemberRole;

public sealed record UpdateWorkspaceMemberRoleCommand(
    Guid UserId,
    Guid WorkspaceId,
    Guid TargetUserId,
    UpdateWorkspaceMemberRoleRequest Request) : IRequest<WorkspaceMemberSummary>;
