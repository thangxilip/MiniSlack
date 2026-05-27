using MediatR;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.CreateConversation;

public sealed class CreateConversationCommandHandler
    : IRequestHandler<CreateConversationCommand, ConversationSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public CreateConversationCommandHandler(
        IWorkspaceCommandStore commandStore,
        IRealtimeNotifier realtimeNotifier)
    {
        _commandStore = commandStore;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<ConversationSummary> Handle(
        CreateConversationCommand command,
        CancellationToken cancellationToken)
    {
        var conversation = await _commandStore.CreateConversationAsync(
            command.UserId,
            command.WorkspaceId,
            command.Request,
            cancellationToken);

        await _realtimeNotifier.ConversationCreatedAsync(
            conversation,
            [command.UserId],
            cancellationToken);

        return conversation;
    }
}
