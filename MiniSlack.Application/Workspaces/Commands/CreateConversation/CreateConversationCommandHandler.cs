using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.CreateConversation;

public sealed class CreateConversationCommandHandler
    : IRequestHandler<CreateConversationCommand, ConversationSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;

    public CreateConversationCommandHandler(IWorkspaceCommandStore commandStore)
    {
        _commandStore = commandStore;
    }

    public Task<ConversationSummary> Handle(
        CreateConversationCommand command,
        CancellationToken cancellationToken)
    {
        return _commandStore.CreateConversationAsync(
            command.UserId,
            command.WorkspaceId,
            command.Request,
            cancellationToken);
    }
}
