using MiniSlack.Domain.Common;

namespace MiniSlack.Domain.Messages;

public sealed class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }

    public Message? Message { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string BlobUrl { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}
