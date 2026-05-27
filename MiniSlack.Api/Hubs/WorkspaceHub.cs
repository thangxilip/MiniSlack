using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Hubs;

[Authorize]
public sealed class WorkspaceHub : Hub<IWorkspaceClient>
{
    private readonly IWorkspaceReadStore _readStore;
    private readonly IUserPresenceTracker _presenceTracker;

    public WorkspaceHub(
        IWorkspaceReadStore readStore,
        IUserPresenceTracker presenceTracker)
    {
        _readStore = readStore;
        _presenceTracker = presenceTracker;
    }

    public override async Task OnConnectedAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.User(userId));
        var becameOnline = _presenceTracker.AddConnection(userId, Context.ConnectionId);
        if (becameOnline)
        {
            await BroadcastPresenceAsync(userId, true, Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetCurrentUserId(out var userId))
        {
            var becameOffline = _presenceTracker.RemoveConnection(userId, Context.ConnectionId);
            if (becameOffline)
            {
                await BroadcastPresenceAsync(userId, false, CancellationToken.None);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinWorkspace(Guid workspaceId)
    {
        if (!TryGetCurrentUserId(out var userId)
            || !await _readStore.IsWorkspaceMemberAsync(userId, workspaceId, Context.ConnectionAborted))
        {
            throw new HubException("You do not have access to this workspace.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.Workspace(workspaceId),
            Context.ConnectionAborted);
    }

    public Task LeaveWorkspace(Guid workspaceId)
    {
        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.Workspace(workspaceId),
            Context.ConnectionAborted);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        if (!TryGetCurrentUserId(out var userId)
            || !await _readStore.IsConversationMemberAsync(userId, conversationId, Context.ConnectionAborted))
        {
            throw new HubException("You do not have access to this conversation.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.Conversation(conversationId),
            Context.ConnectionAborted);
    }

    public Task LeaveConversation(Guid conversationId)
    {
        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.Conversation(conversationId),
            Context.ConnectionAborted);
    }

    public Task StartTyping(Guid conversationId)
    {
        return BroadcastTypingAsync(conversationId, isTyping: true);
    }

    public Task StopTyping(Guid conversationId)
    {
        return BroadcastTypingAsync(conversationId, isTyping: false);
    }

    private async Task BroadcastTypingAsync(
        Guid conversationId,
        bool isTyping)
    {
        if (!TryGetCurrentUserId(out var userId)
            || !await _readStore.IsConversationMemberAsync(userId, conversationId, Context.ConnectionAborted))
        {
            throw new HubException("You do not have access to this conversation.");
        }

        var displayName = await _readStore.GetUserDisplayNameAsync(userId, Context.ConnectionAborted);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        var typing = new TypingRealtimeDto(conversationId, userId, displayName);
        var recipients = Clients.OthersInGroup(RealtimeGroups.Conversation(conversationId));

        if (isTyping)
        {
            await recipients.UserTyping(typing);
            return;
        }

        await recipients.UserStoppedTyping(typing);
    }

    private async Task BroadcastPresenceAsync(
        Guid userId,
        bool isOnline,
        CancellationToken cancellationToken)
    {
        var workspaceIds = await _readStore.GetWorkspaceIdsForUserAsync(userId, cancellationToken);
        var presence = new PresenceRealtimeDto(userId, isOnline);

        foreach (var workspaceId in workspaceIds)
        {
            await Clients.Group(RealtimeGroups.Workspace(workspaceId)).MemberPresenceChanged(presence);
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        return Guid.TryParse(value, out userId);
    }
}
