using System.Text.Json;
using WebPush;

namespace ICQ.Server.Services;

public class WebPushService
{
    private readonly VapidDetails _vapid;
    private readonly WebPushClient _client;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(IConfiguration config, ILogger<WebPushService> logger)
    {
        _logger = logger;
        var publicKey = config["WebPush:PublicKey"] ?? "";
        var privateKey = config["WebPush:PrivateKey"] ?? "";
        var subject = config["WebPush:Subject"] ?? "mailto:admin@icqgrok.local";
        _vapid = new VapidDetails(subject, publicKey, privateKey);
        _client = new WebPushClient();
    }

    public async Task SendAsync(string endpoint, string p256dh, string auth, string title, string body, int? chatId = null)
    {
        if (string.IsNullOrEmpty(_vapid.PublicKey) || string.IsNullOrEmpty(_vapid.PrivateKey))
        {
            _logger.LogDebug("WebPush VAPID keys not configured, skip");
            return;
        }

        var subscription = new PushSubscription(endpoint, p256dh, auth);
        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            chatId,
            icon = "/web/icons/icon-192.png",
            badge = "/web/icons/icon-192.png"
        });

        try
        {
            await _client.SendNotificationAsync(subscription, payload, _vapid);
        }
        catch (WebPushException ex)
        {
            _logger.LogWarning(ex, "WebPush send failed: {Status}", ex.StatusCode);
            throw;
        }
    }

    public string? GetPublicKey() => string.IsNullOrEmpty(_vapid.PublicKey) ? null : _vapid.PublicKey;
}
