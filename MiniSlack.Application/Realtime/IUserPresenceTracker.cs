namespace MiniSlack.Application.Realtime;

public interface IUserPresenceTracker
{
    bool AddConnection(
        Guid userId,
        string connectionId);

    bool RemoveConnection(
        Guid userId,
        string connectionId);

    bool IsOnline(Guid userId);
}
