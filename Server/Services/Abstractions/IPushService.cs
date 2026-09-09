using ICQ.Server.Models;

namespace ICQ.Server.Services.Abstractions;

public interface IPushService
{
    Task RegisterDeviceAsync(Guid userId, string token, string platform);
    Task UnregisterDeviceAsync(string token);
    Task NotifyNewMessageAsync(Guid recipientUserId, MessageDto message, string senderNickname);
}
