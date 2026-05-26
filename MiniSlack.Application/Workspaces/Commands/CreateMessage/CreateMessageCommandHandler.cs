using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.CreateMessage;

public sealed class CreateMessageCommandHandler
    : IRequestHandler<CreateMessageCommand, MessageSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;

    public CreateMessageCommandHandler(IWorkspaceCommandStore commandStore)
    {
        _commandStore = commandStore;
    }

    public Task<MessageSummary> Handle(
        CreateMessageCommand command,
        CancellationToken cancellationToken)
    {
        return _commandStore.CreateMessageAsync(
            command.UserId,
            command.ConversationId,
            command.Request,
            cancellationToken);
    }
}
