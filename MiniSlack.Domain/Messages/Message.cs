using MiniSlack.Domain.Common;
using MiniSlack.Domain.Conversations;
using MiniSlack.Domain.Users;

namespace MiniSlack.Domain.Messages;

public sealed class Message : BaseEntity
{
    public Guid ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public Guid SenderId { get; set; }

    public User? Sender { get; set; }

    public Guid? ParentMessageId { get; set; }

    public Message? ParentMessage { get; set; }

    public string Content { get; set; } = string.Empty;

    public MessageType Type { get; set; }

    public DateTimeOffset? EditedAtUtc { get; set; }

    public ICollection<Message> Replies { get; set; } = [];

    public ICollection<MessageReaction> Reactions { get; set; } = [];

    public ICollection<MessageAttachment> Attachments { get; set; } = [];
}
