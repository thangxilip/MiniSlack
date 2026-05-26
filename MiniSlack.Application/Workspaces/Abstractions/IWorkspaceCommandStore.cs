namespace MiniSlack.Application.Workspaces.Abstractions;

public interface IWorkspaceCommandStore
{
    Task EnsureDefaultWorkspaceAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<WorkspaceSummary> CreateWorkspaceAsync(
        Guid userId,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken);

    Task<ConversationSummary> CreateConversationAsync(
        Guid userId,
        Guid workspaceId,
        CreateConversationRequest request,
        CancellationToken cancellationToken);

    Task<ConversationSummary> StartDirectMessageAsync(
        Guid userId,
        Guid workspaceId,
        StartDirectMessageRequest request,
        CancellationToken cancellationToken);

    Task<MessageSummary> CreateMessageAsync(
        Guid userId,
        Guid conversationId,
        CreateMessageRequest request,
        CancellationToken cancellationToken);
}
