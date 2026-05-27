namespace MiniSlack.Hubs;

public static class RealtimeGroups
{
    public static string Workspace(Guid workspaceId) => $"workspace:{workspaceId}";

    public static string Conversation(Guid conversationId) => $"conversation:{conversationId}";

    public static string User(Guid userId) => $"user:{userId}";
}
