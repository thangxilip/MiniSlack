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

    Task<CreatedWorkspaceInviteSummary> CreateWorkspaceInviteAsync(
        Guid userId,
        Guid workspaceId,
        CreateWorkspaceInviteRequest request,
        string acceptUrlBase,
        CancellationToken cancellationToken);

    Task<AcceptWorkspaceInviteResult> AcceptWorkspaceInviteAsync(
        Guid userId,
        AcceptWorkspaceInviteRequest request,
        CancellationToken cancellationToken);

    Task RevokeWorkspaceInviteAsync(
        Guid userId,
        Guid workspaceId,
        Guid inviteId,
        CancellationToken cancellationToken);

    Task<RemovedWorkspaceMemberSummary> RemoveWorkspaceMemberAsync(
        Guid userId,
        Guid workspaceId,
        Guid targetUserId,
        CancellationToken cancellationToken);

    Task<WorkspaceMemberSummary> UpdateWorkspaceMemberRoleAsync(
        Guid userId,
        Guid workspaceId,
        Guid targetUserId,
        UpdateWorkspaceMemberRoleRequest request,
        CancellationToken cancellationToken);
}
