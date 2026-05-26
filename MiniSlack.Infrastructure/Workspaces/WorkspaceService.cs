using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniSlack.Application.Workspaces;
using MiniSlack.Domain.Conversations;
using MiniSlack.Domain.Messages;
using MiniSlack.Domain.Workspaces;
using MiniSlack.Infrastructure.Persistence;

namespace MiniSlack.Infrastructure.Workspaces;

public sealed partial class WorkspaceService : IWorkspaceService
{
    private const int MaxMessagePageSize = 100;
    private readonly AppDbContext _dbContext;

    public WorkspaceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WorkspaceSummary>> GetWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultWorkspaceAsync(userId, cancellationToken);

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

    public async Task<WorkspaceSummary> CreateWorkspaceAsync(
        Guid userId,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = NormalizeRequiredText(request.Name, 120, "Workspace name is required.");
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = await BuildUniqueSlugAsync(name, cancellationToken),
            OwnerUserId = userId
        };

        var workspaceMember = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = userId,
            Role = WorkspaceMemberRole.Owner,
            JoinedAtUtc = DateTimeOffset.UtcNow
        };

        var general = CreateChannel(workspace.Id, userId, "general", "Workspace-wide updates and discussion.");
        var generalMember = CreateConversationMember(general.Id, userId, ConversationMemberRole.Admin);

        _dbContext.Workspaces.Add(workspace);
        _dbContext.WorkspaceMembers.Add(workspaceMember);
        _dbContext.Conversations.Add(general);
        _dbContext.ConversationMembers.Add(generalMember);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new WorkspaceSummary(workspace.Id, workspace.Name, workspace.Slug, workspaceMember.Role);
    }

    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
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
                conversation.Name ?? "direct message",
                conversation.Description,
                conversation.IsPrivate,
                conversation.Members.Count,
                conversation.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationSummary> CreateConversationAsync(
        Guid userId,
        Guid workspaceId,
        CreateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureWorkspaceMemberAsync(userId, workspaceId, cancellationToken);

        var name = NormalizeRequiredText(request.Name, 120, "Conversation name is required.");
        var normalizedName = SlugPartExpression().Replace(name.Trim().ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Conversation name must include at least one letter or number.");
        }

        var exists = await _dbContext.Conversations.AnyAsync(
            conversation => conversation.WorkspaceId == workspaceId
                && conversation.Type == request.Type
                && conversation.Name == normalizedName,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A conversation with this name already exists.");
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Type = request.Type,
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            IsPrivate = request.IsPrivate,
            CreatorUserId = userId
        };

        _dbContext.Conversations.Add(conversation);
        _dbContext.ConversationMembers.Add(CreateConversationMember(
            conversation.Id,
            userId,
            ConversationMemberRole.Admin));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ConversationSummary(
            conversation.Id,
            conversation.WorkspaceId,
            conversation.Type,
            conversation.Name,
            conversation.Description,
            conversation.IsPrivate,
            1,
            conversation.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<MessageSummary>> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken = default)
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

    public async Task<MessageSummary> CreateMessageAsync(
        Guid userId,
        Guid conversationId,
        CreateMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureConversationMemberAsync(userId, conversationId, cancellationToken);

        var content = NormalizeRequiredText(request.Content, 4000, "Message content is required.");
        if (request.ParentMessageId is not null)
        {
            var parentExists = await _dbContext.Messages.AnyAsync(
                message => message.Id == request.ParentMessageId && message.ConversationId == conversationId,
                cancellationToken);

            if (!parentExists)
            {
                throw new InvalidOperationException("Parent message was not found in this conversation.");
            }
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = userId,
            ParentMessageId = request.ParentMessageId,
            Content = content,
            Type = MessageType.Text
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var sender = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.DisplayName,
                user.AvatarUrl
            })
            .SingleAsync(cancellationToken);

        return new MessageSummary(
            message.Id,
            message.ConversationId,
            message.SenderId,
            sender.DisplayName,
            sender.AvatarUrl,
            message.Content,
            message.Type,
            message.CreatedAtUtc,
            message.EditedAtUtc);
    }

    private async Task EnsureDefaultWorkspaceAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var hasWorkspace = await _dbContext.WorkspaceMembers.AnyAsync(
            member => member.UserId == userId,
            cancellationToken);

        if (hasWorkspace)
        {
            return;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        await CreateWorkspaceAsync(
            userId,
            new CreateWorkspaceRequest($"{user.DisplayName}'s Workspace"),
            cancellationToken);
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

    private async Task<string> BuildUniqueSlugAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var baseSlug = SlugPartExpression()
            .Replace(name.Trim().ToLowerInvariant(), "-")
            .Trim('-');

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "workspace";
        }

        if (baseSlug.Length > 80)
        {
            baseSlug = baseSlug[..80].Trim('-');
        }

        var slug = baseSlug;
        var suffix = 2;
        while (await _dbContext.Workspaces.AnyAsync(workspace => workspace.Slug == slug, cancellationToken))
        {
            var suffixText = $"-{suffix}";
            var prefix = baseSlug.Length + suffixText.Length > 80
                ? baseSlug[..(80 - suffixText.Length)].Trim('-')
                : baseSlug;

            slug = $"{prefix}{suffixText}";
            suffix++;
        }

        return slug;
    }

    private static Conversation CreateChannel(
        Guid workspaceId,
        Guid creatorUserId,
        string name,
        string description)
    {
        return new Conversation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Type = ConversationType.Channel,
            Name = name,
            Description = description,
            CreatorUserId = creatorUserId
        };
    }

    private static ConversationMember CreateConversationMember(
        Guid conversationId,
        Guid userId,
        ConversationMemberRole role)
    {
        return new ConversationMember
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            UserId = userId,
            Role = role,
            JoinedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static string NormalizeRequiredText(
        string value,
        int maxLength,
        string requiredMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(requiredMessage);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugPartExpression();
}
