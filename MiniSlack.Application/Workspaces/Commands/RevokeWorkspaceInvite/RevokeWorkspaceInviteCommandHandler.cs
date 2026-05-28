using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.RevokeWorkspaceInvite;

public sealed class RevokeWorkspaceInviteCommandHandler
    : IRequestHandler<RevokeWorkspaceInviteCommand>
{
    private readonly IWorkspaceCommandStore _commandStore;

    public RevokeWorkspaceInviteCommandHandler(IWorkspaceCommandStore commandStore)
    {
        _commandStore = commandStore;
    }

    public Task Handle(
        RevokeWorkspaceInviteCommand command,
        CancellationToken cancellationToken)
    {
        return _commandStore.RevokeWorkspaceInviteAsync(
            command.UserId,
            command.WorkspaceId,
            command.InviteId,
            cancellationToken);
    }
}
