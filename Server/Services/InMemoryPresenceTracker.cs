using System.Collections.Concurrent;
using ICQ.Server.Services.Abstractions;

namespace ICQ.Server.Services;

/// <summary>Single-process presence. Not shared across scaled-out instances.</summary>
public sealed class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _online = new();

    public void AddConnection(Guid userId, string connectionId)
    {
        var set = _online.GetOrAdd(userId, static _ => new ConcurrentDictionary<string, byte>());
        set[connectionId] = 0;
    }

    public bool RemoveConnection(Guid userId, string connectionId)
    {
        if (!_online.TryGetValue(userId, out var set))
            return false;

        set.TryRemove(connectionId, out _);
        if (!set.IsEmpty)
            return false;

        _online.TryRemove(userId, out _);
        return true;
    }

    public IReadOnlyList<string> GetConnections(Guid userId) =>
        _online.TryGetValue(userId, out var set) ? set.Keys.ToList() : Array.Empty<string>();

    public bool IsOnline(Guid userId) =>
        _online.TryGetValue(userId, out var set) && !set.IsEmpty;

    public IReadOnlyCollection<Guid> OnlineUserIds => _online.Keys.ToList();
}
