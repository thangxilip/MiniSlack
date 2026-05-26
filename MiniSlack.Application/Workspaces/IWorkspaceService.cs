namespace MiniSlack.Application.Workspaces;

public interface IWorkspaceService
{
    Task<IReadOnlyList<WorkspaceSummary>> GetWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceSummary> CreateWorkspaceAsync(
        Guid userId,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<ConversationSummary> CreateConversationAsync(
        Guid userId,
        Guid workspaceId,
        CreateConversationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageSummary>> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken = default);

    Task<MessageSummary> CreateMessageAsync(
        Guid userId,
        Guid conversationId,
        CreateMessageRequest request,
        CancellationToken cancellationToken = default);
}
