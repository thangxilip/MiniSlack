using MiniSlack.Domain.Common;
using MiniSlack.Domain.Users;

namespace MiniSlack.Domain.Messages;

public sealed class MessageReaction : BaseEntity
{
    public Guid MessageId { get; set; }

    public Message? Message { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string Emoji { get; set; } = string.Empty;
}
