using MediatR;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.CreateMessage;

public sealed class CreateMessageCommandHandler
    : IRequestHandler<CreateMessageCommand, MessageSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public CreateMessageCommandHandler(
        IWorkspaceCommandStore commandStore,
        IRealtimeNotifier realtimeNotifier)
    {
        _commandStore = commandStore;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<MessageSummary> Handle(
        CreateMessageCommand command,
        CancellationToken cancellationToken)
    {
        var message = await _commandStore.CreateMessageAsync(
            command.UserId,
            command.ConversationId,
            command.Request,
            cancellationToken);

        await _realtimeNotifier.MessageCreatedAsync(message, cancellationToken);

        return message;
    }
}
