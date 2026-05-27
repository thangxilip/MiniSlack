using System.Collections.Concurrent;
using MiniSlack.Application.Realtime;

namespace MiniSlack.Infrastructure.Realtime;

public sealed class InMemoryUserPresenceTracker : IUserPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();
    private readonly object _gate = new();

    public bool AddConnection(
        Guid userId,
        string connectionId)
    {
        lock (_gate)
        {
            var connections = _connections.GetOrAdd(userId, _ => []);
            var wasOffline = connections.Count == 0;
            connections.Add(connectionId);
            return wasOffline;
        }
    }

    public bool RemoveConnection(
        Guid userId,
        string connectionId)
    {
        lock (_gate)
        {
            if (!_connections.TryGetValue(userId, out var connections))
            {
                return false;
            }

            connections.Remove(connectionId);
            if (connections.Count > 0)
            {
                return false;
            }

            _connections.TryRemove(userId, out _);
            return true;
        }
    }

    public bool IsOnline(Guid userId)
    {
        lock (_gate)
        {
            return _connections.TryGetValue(userId, out var connections)
                && connections.Count > 0;
        }
    }
}
