using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.RevokeWorkspaceInvite;

public sealed record RevokeWorkspaceInviteCommand(
    Guid UserId,
    Guid WorkspaceId,
    Guid InviteId) : IRequest;
