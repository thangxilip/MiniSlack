using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.CreateWorkspace;

public sealed class CreateWorkspaceCommandHandler
    : IRequestHandler<CreateWorkspaceCommand, WorkspaceSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;

    public CreateWorkspaceCommandHandler(IWorkspaceCommandStore commandStore)
    {
        _commandStore = commandStore;
    }

    public Task<WorkspaceSummary> Handle(
        CreateWorkspaceCommand command,
        CancellationToken cancellationToken)
    {
        return _commandStore.CreateWorkspaceAsync(
            command.UserId,
            command.Request,
            cancellationToken);
    }
}
