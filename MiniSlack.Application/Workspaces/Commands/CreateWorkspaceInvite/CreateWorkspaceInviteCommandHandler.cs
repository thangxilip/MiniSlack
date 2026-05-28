using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.CreateWorkspaceInvite;

public sealed class CreateWorkspaceInviteCommandHandler
    : IRequestHandler<CreateWorkspaceInviteCommand, CreatedWorkspaceInviteSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;

    public CreateWorkspaceInviteCommandHandler(IWorkspaceCommandStore commandStore)
    {
        _commandStore = commandStore;
    }

    public Task<CreatedWorkspaceInviteSummary> Handle(
        CreateWorkspaceInviteCommand command,
        CancellationToken cancellationToken)
    {
        return _commandStore.CreateWorkspaceInviteAsync(
            command.UserId,
            command.WorkspaceId,
            command.Request,
            command.AcceptUrlBase,
            cancellationToken);
    }
}
