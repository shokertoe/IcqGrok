using System.Text.Json;
using ICQ.Server.Data;
using ICQ.Server.Models;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace ICQ.Server.Services;

public class WebPushService
{
    private readonly AppDbContext _db;
    private readonly ILogger<WebPushService> _logger;
    private readonly VapidDetails _vapid;
    private readonly WebPushClient _client = new();

    public string PublicKey => _vapid.PublicKey;

    public WebPushService(AppDbContext db, IConfiguration config, ILogger<WebPushService> logger)
    {
        _db = db;
        _logger = logger;

        var subject = config["WebPush:Subject"] ?? "mailto:admin@icq.local";
        var publicKey = config["WebPush:PublicKey"];
        var privateKey = config["WebPush:PrivateKey"];

        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
        {
            var keys = VapidHelper.GenerateVapidKeys();
            publicKey = keys.PublicKey;
            privateKey = keys.PrivateKey;
            _logger.LogWarning(
                "Generated ephemeral VAPID keys. Set WebPush:PublicKey / WebPush:PrivateKey for production.\nPublic: {Pub}",
                publicKey);
        }

        _vapid = new VapidDetails(subject, publicKey, privateKey);
    }

    public async Task SaveSubscriptionAsync(Guid userId, WebPushSubscriptionDto sub)
    {
        var existing = await _db.WebPushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == sub.Endpoint);

        if (existing is not null)
        {
            existing.UserId = userId;
            existing.P256dh = sub.Keys.P256dh;
            existing.Auth = sub.Keys.Auth;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.WebPushSubscriptions.Add(new WebPushSubscription
            {
                UserId = userId,
                Endpoint = sub.Endpoint,
                P256dh = sub.Keys.P256dh,
                Auth = sub.Keys.Auth
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task RemoveSubscriptionAsync(string endpoint)
    {
        var existing = await _db.WebPushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        if (existing is not null)
        {
            _db.WebPushSubscriptions.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }

    public async Task NotifyAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null)
    {
        var subs = await _db.WebPushSubscriptions.Where(s => s.UserId == userId).ToListAsync();
        if (subs.Count == 0) return;

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            data = data ?? new Dictionary<string, string>(),
            icon = "/web/icons/icon-192.png",
            badge = "/web/icons/icon-192.png"
        });

        foreach (var sub in subs)
        {
            var pushSub = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
            try
            {
                await _client.SendNotificationAsync(pushSub, payload, _vapid);
            }
            catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Gone
                                               or System.Net.HttpStatusCode.NotFound)
            {
                _db.WebPushSubscriptions.Remove(sub);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WebPush failed for {Endpoint}", sub.Endpoint);
            }
        }
    }
}
