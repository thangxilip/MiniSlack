using Microsoft.EntityFrameworkCore;
using MiniSlack.Application.Workspaces;
using MiniSlack.Application.Workspaces.Abstractions;
using MiniSlack.Infrastructure.Persistence;

namespace MiniSlack.Infrastructure.Workspaces.Stores;

public sealed class WorkspaceReadStore : IWorkspaceReadStore
{
    private const int MaxMessagePageSize = 100;
    private readonly AppDbContext _dbContext;

    public WorkspaceReadStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WorkspaceSummary>> GetWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .OrderBy(member => member.Workspace!.Name)
            .Select(member => new WorkspaceSummary(
                member.WorkspaceId,
                member.Workspace!.Name,
                member.Workspace.Slug,
                member.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        await EnsureWorkspaceMemberAsync(userId, workspaceId, cancellationToken);

        return await _dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.WorkspaceId == workspaceId
                && conversation.Members.Any(member => member.UserId == userId))
            .OrderBy(conversation => conversation.Type)
            .ThenBy(conversation => conversation.Name)
            .Select(conversation => new ConversationSummary(
                conversation.Id,
                conversation.WorkspaceId,
                conversation.Type,
                conversation.Type == MiniSlack.Domain.Conversations.ConversationType.Direct
                    ? conversation.Members
                        .Where(member => member.UserId != userId)
                        .Select(member => member.User!.DisplayName)
                        .FirstOrDefault() ?? conversation.Name ?? "direct message"
                    : conversation.Name ?? "direct message",
                conversation.Description,
                conversation.IsPrivate,
                conversation.Members.Count,
                conversation.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceMemberSummary>> GetWorkspaceMembersAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        await EnsureWorkspaceMemberAsync(userId, workspaceId, cancellationToken);

        return await _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.WorkspaceId == workspaceId)
            .OrderBy(member => member.User!.DisplayName)
            .Select(member => new WorkspaceMemberSummary(
                member.UserId,
                member.User!.DisplayName,
                member.User.Email,
                member.User.AvatarUrl,
                member.User.Status,
                member.Role,
                member.JoinedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MessageSummary>> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken)
    {
        await EnsureConversationMemberAsync(userId, conversationId, cancellationToken);

        var pageSize = Math.Clamp(limit, 1, MaxMessagePageSize);
        var query = _dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId);

        if (before is not null)
        {
            query = query.Where(message => message.CreatedAtUtc < before);
        }

        var messages = await query
            .OrderByDescending(message => message.CreatedAtUtc)
            .Take(pageSize)
            .Select(message => new MessageSummary(
                message.Id,
                message.ConversationId,
                message.SenderId,
                message.Sender!.DisplayName,
                message.Sender.AvatarUrl,
                message.Content,
                message.Type,
                message.CreatedAtUtc,
                message.EditedAtUtc))
            .ToListAsync(cancellationToken);

        messages.Reverse();
        return messages;
    }

    public Task<bool> IsWorkspaceMemberAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkspaceMembers.AnyAsync(
            member => member.WorkspaceId == workspaceId && member.UserId == userId,
            cancellationToken);
    }

    public Task<bool> IsConversationMemberAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ConversationMembers.AnyAsync(
            member => member.ConversationId == conversationId && member.UserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetWorkspaceIdsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .Select(member => member.WorkspaceId)
            .ToListAsync(cancellationToken);
    }

    public Task<string?> GetUserDisplayNameAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureWorkspaceMemberAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var isMember = await _dbContext.WorkspaceMembers.AnyAsync(
            member => member.WorkspaceId == workspaceId && member.UserId == userId,
            cancellationToken);

        if (!isMember)
        {
            throw new UnauthorizedAccessException("You do not have access to this workspace.");
        }
    }

    private async Task EnsureConversationMemberAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var isMember = await _dbContext.ConversationMembers.AnyAsync(
            member => member.ConversationId == conversationId && member.UserId == userId,
            cancellationToken);

        if (!isMember)
        {
            throw new UnauthorizedAccessException("You do not have access to this conversation.");
        }
    }
}
