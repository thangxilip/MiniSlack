namespace MiniSlack.Application.Workspaces.Abstractions;

public interface IWorkspaceReadStore
{
    Task<IReadOnlyList<WorkspaceSummary>> GetWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MessageSummary>> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken);
}
