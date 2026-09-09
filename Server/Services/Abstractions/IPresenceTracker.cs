namespace ICQ.Server.Services.Abstractions;

/// <summary>
/// Tracks which users are connected (and with which connection ids).
/// Default implementation is process-local; replace with Redis for multi-instance.
/// </summary>
public interface IPresenceTracker
{
    void AddConnection(Guid userId, string connectionId);

    /// <returns>True if the user has no remaining connections (went offline).</returns>
    bool RemoveConnection(Guid userId, string connectionId);

    IReadOnlyList<string> GetConnections(Guid userId);
    bool IsOnline(Guid userId);
    IReadOnlyCollection<Guid> OnlineUserIds { get; }
}
