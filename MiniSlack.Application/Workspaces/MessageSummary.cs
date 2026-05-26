using MiniSlack.Domain.Messages;

namespace MiniSlack.Application.Workspaces;

public sealed record MessageSummary(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderDisplayName,
    string? SenderAvatarUrl,
    string Content,
    MessageType Type,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EditedAtUtc);
