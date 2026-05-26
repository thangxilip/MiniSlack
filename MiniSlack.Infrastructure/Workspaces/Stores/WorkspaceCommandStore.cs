using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniSlack.Application.Workspaces;
using MiniSlack.Application.Workspaces.Abstractions;
using MiniSlack.Domain.Conversations;
using MiniSlack.Domain.Messages;
using MiniSlack.Domain.Users;
using MiniSlack.Domain.Workspaces;
using MiniSlack.Infrastructure.Persistence;

namespace MiniSlack.Infrastructure.Workspaces.Stores;

public sealed partial class WorkspaceCommandStore : IWorkspaceCommandStore
{
    private readonly AppDbContext _dbContext;

    public WorkspaceCommandStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureDefaultWorkspaceAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var hasDemoWorkspace = await _dbContext.WorkspaceMembers.AnyAsync(
            member => member.UserId == userId && member.Workspace!.Name == "MiniSlack Team",
            cancellationToken);

        if (hasDemoWorkspace)
        {
            return;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        await CreateDemoWorkspaceAsync(user, cancellationToken);
    }

    public async Task<WorkspaceSummary> CreateWorkspaceAsync(
        Guid userId,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken)
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

    public async Task<ConversationSummary> CreateConversationAsync(
        Guid userId,
        Guid workspaceId,
        CreateConversationRequest request,
        CancellationToken cancellationToken)
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

    public async Task<ConversationSummary> StartDirectMessageAsync(
        Guid userId,
        Guid workspaceId,
        StartDirectMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TargetUserId == userId)
        {
            throw new InvalidOperationException("You cannot start a direct message with yourself.");
        }

        await EnsureWorkspaceMemberAsync(userId, workspaceId, cancellationToken);
        await EnsureWorkspaceMemberAsync(request.TargetUserId, workspaceId, cancellationToken);

        var existingConversation = await _dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.WorkspaceId == workspaceId
                && conversation.Type == ConversationType.Direct
                && conversation.Members.Count == 2
                && conversation.Members.Any(member => member.UserId == userId)
                && conversation.Members.Any(member => member.UserId == request.TargetUserId))
            .Select(conversation => new ConversationSummary(
                conversation.Id,
                conversation.WorkspaceId,
                conversation.Type,
                conversation.Members
                    .Where(member => member.UserId == request.TargetUserId)
                    .Select(member => member.User!.DisplayName)
                    .Single(),
                conversation.Description,
                conversation.IsPrivate,
                conversation.Members.Count,
                conversation.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if (existingConversation is not null)
        {
            return existingConversation;
        }

        var targetUser = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == request.TargetUserId)
            .Select(user => new
            {
                user.DisplayName
            })
            .SingleAsync(cancellationToken);

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Type = ConversationType.Direct,
            Name = targetUser.DisplayName,
            Description = $"Direct message with {targetUser.DisplayName}",
            IsPrivate = true,
            CreatorUserId = userId
        };

        _dbContext.Conversations.Add(conversation);
        _dbContext.ConversationMembers.Add(CreateConversationMember(
            conversation.Id,
            userId,
            ConversationMemberRole.Member));
        _dbContext.ConversationMembers.Add(CreateConversationMember(
            conversation.Id,
            request.TargetUserId,
            ConversationMemberRole.Member));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ConversationSummary(
            conversation.Id,
            conversation.WorkspaceId,
            conversation.Type,
            targetUser.DisplayName,
            conversation.Description,
            conversation.IsPrivate,
            2,
            conversation.CreatedAtUtc);
    }

    public async Task<MessageSummary> CreateMessageAsync(
        Guid userId,
        Guid conversationId,
        CreateMessageRequest request,
        CancellationToken cancellationToken)
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

    private async Task CreateDemoWorkspaceAsync(
        User owner,
        CancellationToken cancellationToken)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "MiniSlack Team",
            Slug = await BuildUniqueSlugAsync("MiniSlack Team", cancellationToken),
            OwnerUserId = owner.Id
        };

        var demoUsers = CreateDemoUsers(owner.Id);
        var allUsers = new[] { owner }.Concat(demoUsers).ToArray();
        var channels = new[]
        {
            CreateChannel(workspace.Id, owner.Id, "general", "Workspace-wide updates and discussion."),
            CreateChannel(workspace.Id, owner.Id, "engineering", "Build notes, API contracts, and implementation details."),
            CreateChannel(workspace.Id, owner.Id, "product", "Roadmap, launch planning, and user feedback."),
            CreateChannel(workspace.Id, owner.Id, "random", "Small talk, links, and team rituals.")
        };
        var directMessage = new Conversation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Type = ConversationType.Direct,
            Name = "an-nguyen",
            Description = "Direct message with An Nguyen",
            CreatorUserId = owner.Id
        };

        _dbContext.Workspaces.Add(workspace);
        _dbContext.Users.AddRange(demoUsers);

        foreach (var member in CreateWorkspaceMembers(workspace.Id, owner.Id, allUsers))
        {
            _dbContext.WorkspaceMembers.Add(member);
        }

        foreach (var channel in channels)
        {
            _dbContext.Conversations.Add(channel);

            foreach (var user in allUsers)
            {
                _dbContext.ConversationMembers.Add(CreateConversationMember(
                    channel.Id,
                    user.Id,
                    user.Id == owner.Id ? ConversationMemberRole.Admin : ConversationMemberRole.Member));
            }
        }

        _dbContext.Conversations.Add(directMessage);
        _dbContext.ConversationMembers.Add(CreateConversationMember(
            directMessage.Id,
            owner.Id,
            ConversationMemberRole.Member));
        _dbContext.ConversationMembers.Add(CreateConversationMember(
            directMessage.Id,
            demoUsers[0].Id,
            ConversationMemberRole.Member));

        foreach (var message in CreateDemoMessages(channels, directMessage, owner, demoUsers))
        {
            _dbContext.Messages.Add(message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static User[] CreateDemoUsers(Guid ownerUserId)
    {
        var suffix = ownerUserId.ToString("N")[..8];

        return
        [
            CreateDemoUser($"an.nguyen+{suffix}@minislack.dev", "An Nguyen", "online"),
            CreateDemoUser($"linh.tran+{suffix}@minislack.dev", "Linh Tran", "away"),
            CreateDemoUser($"minh.pham+{suffix}@minislack.dev", "Minh Pham", "online"),
            CreateDemoUser($"sarah.lee+{suffix}@minislack.dev", "Sarah Lee", "offline")
        ];
    }

    private static User CreateDemoUser(
        string email,
        string displayName,
        string status)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            IdentityProvider = "demo",
            ExternalId = email,
            Email = email,
            EmailVerified = true,
            DisplayName = displayName,
            Status = status
        };
    }

    private static IEnumerable<WorkspaceMember> CreateWorkspaceMembers(
        Guid workspaceId,
        Guid ownerUserId,
        IEnumerable<User> users)
    {
        foreach (var user in users)
        {
            yield return new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                UserId = user.Id,
                Role = user.Id == ownerUserId ? WorkspaceMemberRole.Owner : WorkspaceMemberRole.Member,
                JoinedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }

    private static IEnumerable<Message> CreateDemoMessages(
        IReadOnlyList<Conversation> channels,
        Conversation directMessage,
        User owner,
        IReadOnlyList<User> demoUsers)
    {
        var now = DateTimeOffset.UtcNow;
        var byName = demoUsers.ToDictionary(user => user.DisplayName);

        return
        [
            CreateMessage(channels[0].Id, byName["An Nguyen"].Id, "Welcome to MiniSlack Team. I added a few channels so we can kick the tires with real data.", now.AddMinutes(-48)),
            CreateMessage(channels[0].Id, byName["Linh Tran"].Id, "I will keep notes in #product while the dashboard gets wired up. Empty states are useful, but this already feels better.", now.AddMinutes(-41)),
            CreateMessage(channels[0].Id, owner.Id, "Nice. I am checking the workspace flow now and will use this channel for smoke testing.", now.AddMinutes(-35)),
            CreateMessage(channels[0].Id, byName["Minh Pham"].Id, "Message history, channel membership, and sender avatars are all coming from the database now.", now.AddMinutes(-29)),

            CreateMessage(channels[1].Id, byName["Minh Pham"].Id, "Backend note: dashboard reads workspaces, conversations, and messages separately. That keeps the first API surface simple.", now.AddMinutes(-63)),
            CreateMessage(channels[1].Id, byName["An Nguyen"].Id, "Frontend note: the Pinia store is the right place to own selected workspace and conversation state.", now.AddMinutes(-54)),
            CreateMessage(channels[1].Id, owner.Id, "Next technical milestone should be making send-message reliable, then SignalR.", now.AddMinutes(-18)),

            CreateMessage(channels[2].Id, byName["Sarah Lee"].Id, "For the first real demo, I want the app to feel collaborative immediately after login.", now.AddMinutes(-71)),
            CreateMessage(channels[2].Id, byName["Linh Tran"].Id, "Agreed. Seeded channels plus a few teammates gives us enough texture to test the UX.", now.AddMinutes(-52)),
            CreateMessage(channels[2].Id, byName["Sarah Lee"].Id, "After that, channel creation and invites are probably the highest leverage.", now.AddMinutes(-24)),

            CreateMessage(channels[3].Id, byName["An Nguyen"].Id, "Tiny celebration: the dashboard no longer opens into a quiet void.", now.AddMinutes(-32)),
            CreateMessage(channels[3].Id, byName["Linh Tran"].Id, "I am putting coffee recommendations here until someone builds threads.", now.AddMinutes(-14)),

            CreateMessage(directMessage.Id, byName["An Nguyen"].Id, "Hey, I set up a direct message too so the sidebar has both channel and DM examples.", now.AddMinutes(-26)),
            CreateMessage(directMessage.Id, owner.Id, "Perfect. This makes it much easier to see what the dashboard is supposed to become.", now.AddMinutes(-11))
        ];
    }

    private static Message CreateMessage(
        Guid conversationId,
        Guid senderId,
        string content,
        DateTimeOffset createdAtUtc)
    {
        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            Type = MessageType.Text,
            CreatedAtUtc = createdAtUtc
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
