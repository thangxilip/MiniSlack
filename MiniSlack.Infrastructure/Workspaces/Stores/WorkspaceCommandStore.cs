using System.Security.Cryptography;
using System.Text;
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
    private const int InviteTokenBytes = 32;
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);
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

    public async Task<CreatedWorkspaceInviteSummary> CreateWorkspaceInviteAsync(
        Guid userId,
        Guid workspaceId,
        CreateWorkspaceInviteRequest request,
        string acceptUrlBase,
        CancellationToken cancellationToken)
    {
        var requesterRole = await GetWorkspaceRoleAsync(userId, workspaceId, cancellationToken)
            ?? throw new UnauthorizedAccessException("You do not have access to this workspace.");

        if (requesterRole is not (WorkspaceMemberRole.Admin or WorkspaceMemberRole.Owner))
        {
            throw new UnauthorizedAccessException("You do not have permission to invite members.");
        }

        if (request.Role == WorkspaceMemberRole.Owner
            || request.Role == WorkspaceMemberRole.Admin && requesterRole != WorkspaceMemberRole.Owner)
        {
            throw new InvalidOperationException("You cannot invite a member with this role.");
        }

        var email = NormalizeEmail(request.Email);
        var normalizedEmail = NormalizeEmailKey(email);
        var alreadyMember = await _dbContext.WorkspaceMembers.AnyAsync(
            member => member.WorkspaceId == workspaceId
                && member.User!.Email.ToUpper() == normalizedEmail,
            cancellationToken);

        if (alreadyMember)
        {
            throw new InvalidOperationException("This email is already a workspace member.");
        }

        var now = DateTimeOffset.UtcNow;
        var existingInvite = await _dbContext.WorkspaceInvites
            .Where(invite => invite.WorkspaceId == workspaceId
                && invite.NormalizedEmail == normalizedEmail
                && invite.AcceptedAtUtc == null
                && invite.RevokedAtUtc == null
                && invite.ExpiresAtUtc > now)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingInvite is not null)
        {
            throw new InvalidOperationException("This email already has a pending invite.");
        }

        var token = GenerateInviteToken();
        var invite = new WorkspaceInvite
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = request.Role,
            TokenHash = HashToken(token),
            InvitedByUserId = userId,
            ExpiresAtUtc = now.Add(InviteLifetime)
        };

        _dbContext.WorkspaceInvites.Add(invite);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var invitedBy = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.DisplayName)
            .SingleAsync(cancellationToken);

        return new CreatedWorkspaceInviteSummary(
            invite.Id,
            invite.WorkspaceId,
            invite.Email,
            invite.Role,
            invite.InvitedByUserId,
            invitedBy,
            invite.ExpiresAtUtc,
            invite.CreatedAtUtc,
            token,
            BuildAcceptUrl(acceptUrlBase, token));
    }

    public async Task<AcceptWorkspaceInviteResult> AcceptWorkspaceInviteAsync(
        Guid userId,
        AcceptWorkspaceInviteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ArgumentException("Invite token is required.");
        }

        var tokenHash = HashToken(request.Token.Trim());
        var now = DateTimeOffset.UtcNow;
        var invite = await _dbContext.WorkspaceInvites
            .Include(candidate => candidate.Workspace)
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken)
            ?? throw new InvalidOperationException("Invite was not found.");

        if (invite.AcceptedAtUtc is not null)
        {
            throw new InvalidOperationException("Invite was already accepted.");
        }

        if (invite.RevokedAtUtc is not null)
        {
            throw new InvalidOperationException("Invite was revoked.");
        }

        if (invite.ExpiresAtUtc <= now)
        {
            throw new InvalidOperationException("Invite has expired.");
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!string.Equals(NormalizeEmailKey(user.Email), invite.NormalizedEmail, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("This invite was sent to a different email address.");
        }

        var existingActiveMember = await _dbContext.WorkspaceMembers.AnyAsync(
            member => member.WorkspaceId == invite.WorkspaceId && member.UserId == userId,
            cancellationToken);

        if (existingActiveMember)
        {
            throw new InvalidOperationException("You are already a member of this workspace.");
        }

        var workspaceMember = await _dbContext.WorkspaceMembers
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                member => member.WorkspaceId == invite.WorkspaceId && member.UserId == userId,
                cancellationToken);

        if (workspaceMember is null)
        {
            workspaceMember = new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = invite.WorkspaceId,
                UserId = userId
            };
            _dbContext.WorkspaceMembers.Add(workspaceMember);
        }

        workspaceMember.Role = invite.Role;
        workspaceMember.JoinedAtUtc = now;
        workspaceMember.IsDeleted = false;
        workspaceMember.DeletedAtUtc = null;
        workspaceMember.DeletedByUserId = null;

        await AddMemberToPublicChannelsAsync(invite.WorkspaceId, userId, cancellationToken);

        invite.AcceptedAtUtc = now;
        invite.AcceptedByUserId = userId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var workspace = invite.Workspace
            ?? await _dbContext.Workspaces.SingleAsync(candidate => candidate.Id == invite.WorkspaceId, cancellationToken);

        var member = new WorkspaceMemberSummary(
            user.Id,
            user.DisplayName,
            user.Email,
            user.AvatarUrl,
            user.Status,
            workspaceMember.Role,
            workspaceMember.JoinedAtUtc);

        return new AcceptWorkspaceInviteResult(
            new WorkspaceSummary(workspace.Id, workspace.Name, workspace.Slug, workspaceMember.Role),
            member);
    }

    public async Task RevokeWorkspaceInviteAsync(
        Guid userId,
        Guid workspaceId,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        await EnsureWorkspaceAdminAsync(userId, workspaceId, cancellationToken);

        var invite = await _dbContext.WorkspaceInvites.SingleOrDefaultAsync(
            candidate => candidate.Id == inviteId && candidate.WorkspaceId == workspaceId,
            cancellationToken)
            ?? throw new InvalidOperationException("Invite was not found.");

        if (invite.AcceptedAtUtc is not null)
        {
            throw new InvalidOperationException("Accepted invites cannot be revoked.");
        }

        if (invite.RevokedAtUtc is null)
        {
            invite.RevokedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<RemovedWorkspaceMemberSummary> RemoveWorkspaceMemberAsync(
        Guid userId,
        Guid workspaceId,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        if (userId == targetUserId)
        {
            throw new InvalidOperationException("You cannot remove yourself from the workspace.");
        }

        var requesterRole = await GetWorkspaceRoleAsync(userId, workspaceId, cancellationToken)
            ?? throw new UnauthorizedAccessException("You do not have access to this workspace.");
        var targetMember = await _dbContext.WorkspaceMembers.SingleOrDefaultAsync(
            member => member.WorkspaceId == workspaceId && member.UserId == targetUserId,
            cancellationToken)
            ?? throw new InvalidOperationException("Workspace member was not found.");

        if (requesterRole == WorkspaceMemberRole.Member
            || requesterRole == WorkspaceMemberRole.Admin && targetMember.Role != WorkspaceMemberRole.Member)
        {
            throw new UnauthorizedAccessException("You do not have permission to remove this member.");
        }

        if (targetMember.Role == WorkspaceMemberRole.Owner)
        {
            await EnsureAnotherOwnerExistsAsync(workspaceId, targetUserId, cancellationToken);
        }

        var conversationMembers = await _dbContext.ConversationMembers
            .Where(member => member.UserId == targetUserId
                && member.Conversation!.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        _dbContext.ConversationMembers.RemoveRange(conversationMembers);
        _dbContext.WorkspaceMembers.Remove(targetMember);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RemovedWorkspaceMemberSummary(workspaceId, targetUserId);
    }

    public async Task<WorkspaceMemberSummary> UpdateWorkspaceMemberRoleAsync(
        Guid userId,
        Guid workspaceId,
        Guid targetUserId,
        UpdateWorkspaceMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var requesterRole = await GetWorkspaceRoleAsync(userId, workspaceId, cancellationToken)
            ?? throw new UnauthorizedAccessException("You do not have access to this workspace.");

        if (requesterRole != WorkspaceMemberRole.Owner)
        {
            throw new UnauthorizedAccessException("Only workspace owners can change member roles.");
        }

        var targetMember = await _dbContext.WorkspaceMembers
            .Include(member => member.User)
            .SingleOrDefaultAsync(
                member => member.WorkspaceId == workspaceId && member.UserId == targetUserId,
                cancellationToken)
            ?? throw new InvalidOperationException("Workspace member was not found.");

        if (targetMember.Role == WorkspaceMemberRole.Owner && request.Role != WorkspaceMemberRole.Owner)
        {
            await EnsureAnotherOwnerExistsAsync(workspaceId, targetUserId, cancellationToken);
        }

        targetMember.Role = request.Role;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new WorkspaceMemberSummary(
            targetMember.UserId,
            targetMember.User!.DisplayName,
            targetMember.User.Email,
            targetMember.User.AvatarUrl,
            targetMember.User.Status,
            targetMember.Role,
            targetMember.JoinedAtUtc);
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

    private async Task EnsureWorkspaceAdminAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var role = await GetWorkspaceRoleAsync(userId, workspaceId, cancellationToken);
        if (role is not (WorkspaceMemberRole.Admin or WorkspaceMemberRole.Owner))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage this workspace.");
        }
    }

    private Task<WorkspaceMemberRole?> GetWorkspaceRoleAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkspaceMembers
            .Where(member => member.WorkspaceId == workspaceId && member.UserId == userId)
            .Select(member => (WorkspaceMemberRole?)member.Role)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureAnotherOwnerExistsAsync(
        Guid workspaceId,
        Guid excludedUserId,
        CancellationToken cancellationToken)
    {
        var hasAnotherOwner = await _dbContext.WorkspaceMembers.AnyAsync(
            member => member.WorkspaceId == workspaceId
                && member.UserId != excludedUserId
                && member.Role == WorkspaceMemberRole.Owner,
            cancellationToken);

        if (!hasAnotherOwner)
        {
            throw new InvalidOperationException("A workspace must have at least one owner.");
        }
    }

    private async Task AddMemberToPublicChannelsAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var publicChannelIds = await _dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.WorkspaceId == workspaceId
                && conversation.Type == ConversationType.Channel
                && !conversation.IsPrivate)
            .Select(conversation => conversation.Id)
            .ToListAsync(cancellationToken);

        foreach (var conversationId in publicChannelIds)
        {
            var member = await _dbContext.ConversationMembers
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    candidate => candidate.ConversationId == conversationId && candidate.UserId == userId,
                    cancellationToken);

            if (member is null)
            {
                _dbContext.ConversationMembers.Add(CreateConversationMember(
                    conversationId,
                    userId,
                    ConversationMemberRole.Member));
                continue;
            }

            member.Role = ConversationMemberRole.Member;
            member.JoinedAtUtc = DateTimeOffset.UtcNow;
            member.IsDeleted = false;
            member.DeletedAtUtc = null;
            member.DeletedByUserId = null;
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

    private static string NormalizeEmail(string value)
    {
        var email = NormalizeRequiredText(value, 320, "Email is required.");
        if (!email.Contains('@', StringComparison.Ordinal) || email.StartsWith('@') || email.EndsWith('@'))
        {
            throw new ArgumentException("A valid email address is required.");
        }

        return email;
    }

    private static string NormalizeEmailKey(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string GenerateInviteToken()
    {
        Span<byte> bytes = stackalloc byte[InviteTokenBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string BuildAcceptUrl(
        string acceptUrlBase,
        string token)
    {
        var separator = acceptUrlBase.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{acceptUrlBase}{separator}token={Uri.EscapeDataString(token)}";
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
