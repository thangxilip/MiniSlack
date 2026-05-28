using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.RemoveWorkspaceMember;

public sealed record RemoveWorkspaceMemberCommand(
    Guid UserId,
    Guid WorkspaceId,
    Guid TargetUserId) : IRequest<RemovedWorkspaceMemberSummary>;
