using Microsoft.AspNetCore.SignalR;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces;
using MiniSlack.Domain.Conversations;
using MiniSlack.Hubs;

namespace MiniSlack.Realtime;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<WorkspaceHub, IWorkspaceClient> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<WorkspaceHub, IWorkspaceClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task MessageCreatedAsync(
        MessageSummary message,
        CancellationToken cancellationToken)
    {
        return _hubContext.Clients
            .Group(RealtimeGroups.Conversation(message.ConversationId))
            .MessageCreated(message);
    }

    public Task ConversationCreatedAsync(
        ConversationSummary conversation,
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken)
    {
        if (conversation.Type == ConversationType.Channel && !conversation.IsPrivate)
        {
            return _hubContext.Clients
                .Group(RealtimeGroups.Workspace(conversation.WorkspaceId))
                .ConversationCreated(conversation);
        }

        var userGroups = recipientUserIds
            .Select(RealtimeGroups.User)
            .ToArray();

        return _hubContext.Clients
            .Groups(userGroups)
            .ConversationCreated(conversation);
    }
}
