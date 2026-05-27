using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces;

namespace MiniSlack.Hubs;

public interface IWorkspaceClient
{
    Task MessageCreated(MessageSummary message);

    Task ConversationCreated(ConversationSummary conversation);

    Task MemberPresenceChanged(PresenceRealtimeDto presence);

    Task UserTyping(TypingRealtimeDto typing);

    Task UserStoppedTyping(TypingRealtimeDto typing);
}
