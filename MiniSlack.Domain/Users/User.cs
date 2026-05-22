using MiniSlack.Domain.Common;
using MiniSlack.Domain.Workspaces;
using MiniSlack.Domain.Conversations;
using MiniSlack.Domain.Messages;

namespace MiniSlack.Domain.Users;

public sealed class User : BaseEntity
{
    public string GoogleId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? Status { get; set; }

    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = [];

    public ICollection<ConversationMember> ConversationMemberships { get; set; } = [];

    public ICollection<Message> Messages { get; set; } = [];

    public ICollection<MessageReaction> MessageReactions { get; set; } = [];
}
