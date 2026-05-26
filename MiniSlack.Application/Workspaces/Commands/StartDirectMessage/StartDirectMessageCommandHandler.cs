using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.StartDirectMessage;

public sealed class StartDirectMessageCommandHandler
    : IRequestHandler<StartDirectMessageCommand, ConversationSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;

    public StartDirectMessageCommandHandler(IWorkspaceCommandStore commandStore)
    {
        _commandStore = commandStore;
    }

    public Task<ConversationSummary> Handle(
        StartDirectMessageCommand command,
        CancellationToken cancellationToken)
    {
        return _commandStore.StartDirectMessageAsync(
            command.UserId,
            command.WorkspaceId,
            command.Request,
            cancellationToken);
    }
}
