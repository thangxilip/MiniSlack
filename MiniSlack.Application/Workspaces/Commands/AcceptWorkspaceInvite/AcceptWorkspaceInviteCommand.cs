using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.AcceptWorkspaceInvite;

public sealed record AcceptWorkspaceInviteCommand(
    Guid UserId,
    AcceptWorkspaceInviteRequest Request) : IRequest<AcceptWorkspaceInviteResult>;
