namespace MiniSlack.Application.Realtime;

public sealed record TypingRealtimeDto(
    Guid ConversationId,
    Guid UserId,
    string DisplayName);
