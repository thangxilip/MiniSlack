namespace MiniSlack.Application.Realtime;

public sealed record PresenceRealtimeDto(
    Guid UserId,
    bool IsOnline);
