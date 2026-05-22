using MiniSlack.Domain.Common;
using MiniSlack.Domain.Messages;
using MiniSlack.Domain.Users;

namespace MiniSlack.Domain.Conversations;

public sealed class ConversationMember : BaseEntity
{
    public Guid ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public ConversationMemberRole Role { get; set; }

    public DateTimeOffset JoinedAtUtc { get; set; }

    public Guid? LastReadMessageId { get; set; }

    public Message? LastReadMessage { get; set; }
}
