using MiniSlack.Domain.Common;
using MiniSlack.Domain.Messages;
using MiniSlack.Domain.Users;
using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Domain.Conversations;

public sealed class Conversation : BaseEntity
{
    public Guid WorkspaceId { get; set; }

    public Workspace? Workspace { get; set; }

    public ConversationType Type { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool IsPrivate { get; set; }

    public Guid CreatorUserId { get; set; }

    public User? CreatorUser { get; set; }

    public ICollection<ConversationMember> Members { get; set; } = [];

    public ICollection<Message> Messages { get; set; } = [];
}
