using MediatR;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.StartDirectMessage;

public sealed class StartDirectMessageCommandHandler
    : IRequestHandler<StartDirectMessageCommand, ConversationSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public StartDirectMessageCommandHandler(
        IWorkspaceCommandStore commandStore,
        IRealtimeNotifier realtimeNotifier)
    {
        _commandStore = commandStore;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<ConversationSummary> Handle(
        StartDirectMessageCommand command,
        CancellationToken cancellationToken)
    {
        var conversation = await _commandStore.StartDirectMessageAsync(
            command.UserId,
            command.WorkspaceId,
            command.Request,
            cancellationToken);

        await _realtimeNotifier.ConversationCreatedAsync(
            conversation,
            [command.UserId, command.Request.TargetUserId],
            cancellationToken);

        return conversation;
    }
}
