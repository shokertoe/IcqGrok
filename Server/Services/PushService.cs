using Microsoft.EntityFrameworkCore;
using ICQ.Server.Data;
using ICQ.Server.Models;

namespace ICQ.Server.Services;

public class PushService
{
    private readonly AppDbContext _db;
    private readonly WebPushService _webPush;
    private readonly ILogger<PushService> _logger;

    public PushService(AppDbContext db, WebPushService webPush, ILogger<PushService> logger)
    {
        _db = db;
        _webPush = webPush;
        _logger = logger;
    }

    public async Task SubscribeAsync(int userId, WebPushSubscribeRequest req)
    {
        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Endpoint == req.Endpoint);

        if (existing != null)
        {
            existing.P256dh = req.P256dh;
            existing.Auth = req.Auth;
            existing.Platform = req.Platform ?? "web";
        }
        else
        {
            _db.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = req.Endpoint,
                P256dh = req.P256dh,
                Auth = req.Auth,
                Platform = req.Platform ?? "web",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task UnsubscribeAsync(int userId, string endpoint)
    {
        var sub = await _db.PushSubscriptions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Endpoint == endpoint);
        if (sub != null)
        {
            _db.PushSubscriptions.Remove(sub);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SendMessagePushAsync(int userId, MessageDto msg)
    {
        var subs = await _db.PushSubscriptions.Where(p => p.UserId == userId).ToListAsync();
        if (subs.Count == 0) return;

        var title = msg.SenderNickname;
        var body = msg.IsEncrypted ? "🔒 Encrypted message" : Smileys.Expand(msg.Content);
        if (body.Length > 120) body = body[..117] + "…";

        foreach (var sub in subs)
        {
            try
            {
                if (sub.Platform == "web" || string.IsNullOrEmpty(sub.Platform))
                {
                    await _webPush.SendAsync(sub.Endpoint, sub.P256dh, sub.Auth, title, body, msg.ChatId);
                }
                // FCM for android/ios can be added here later
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push failed for user {UserId} endpoint {Endpoint}", userId, sub.Endpoint);
                // remove invalid subscriptions on 410 Gone etc. if needed
            }
        }
    }
}
