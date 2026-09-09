using ICQ.Server.Models;

namespace ICQ.Server.Services.Abstractions;

public interface IWebPushService
{
    string PublicKey { get; }
    Task SaveSubscriptionAsync(Guid userId, WebPushSubscriptionDto sub);
    Task RemoveSubscriptionAsync(string endpoint);
    Task NotifyAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null);
}
