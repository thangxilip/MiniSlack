using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.CreateWorkspaceInvite;

public sealed record CreateWorkspaceInviteCommand(
    Guid UserId,
    Guid WorkspaceId,
    CreateWorkspaceInviteRequest Request,
    string AcceptUrlBase) : IRequest<CreatedWorkspaceInviteSummary>;
