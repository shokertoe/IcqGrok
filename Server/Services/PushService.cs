using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ICQ.Server.Data;
using ICQ.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ICQ.Server.Services;

/// <summary>
/// Push notifications via Firebase Cloud Messaging (FCM).
/// Register device tokens with POST /api/push/register.
/// When credentials are not configured, pushes are no-ops (logged only).
/// </summary>
public class PushService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PushService> _logger;
    private readonly bool _enabled;

    public PushService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<PushService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
        _enabled = !string.IsNullOrWhiteSpace(config["Firebase:ServerKey"])
                   || !string.IsNullOrWhiteSpace(config["Firebase:CredentialsPath"]);
    }

    public async Task RegisterDeviceAsync(Guid userId, string token, string platform)
    {
        var existing = await _db.DeviceTokens
            .FirstOrDefaultAsync(d => d.Token == token);

        if (existing is not null)
        {
            existing.UserId = userId;
            existing.Platform = platform;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.DeviceTokens.Add(new DeviceToken
            {
                UserId = userId,
                Token = token,
                Platform = platform
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task UnregisterDeviceAsync(string token)
    {
        var existing = await _db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token);
        if (existing is not null)
        {
            _db.DeviceTokens.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }

    public async Task NotifyNewMessageAsync(Guid recipientUserId, MessageDto message, string senderNickname)
    {
        if (!_enabled)
        {
            _logger.LogDebug("Push disabled — would notify {UserId} about message from {Sender}",
                recipientUserId, senderNickname);
            return;
        }

        var tokens = await _db.DeviceTokens
            .Where(d => d.UserId == recipientUserId)
            .Select(d => d.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        var title = senderNickname;
        var body = message.Type switch
        {
            MessageType.Image => "📷 Image",
            MessageType.File => $"📎 {message.AttachmentName ?? "File"}",
            _ => message.Text?.Length > 100 ? message.Text[..100] + "…" : message.Text ?? ""
        };

        foreach (var token in tokens)
        {
            await SendFcmAsync(token, title, body, new Dictionary<string, string>
            {
                ["chatId"] = message.ChatId.ToString(),
                ["messageId"] = message.Id.ToString(),
                ["type"] = "new_message"
            });
        }
    }

    private async Task SendFcmAsync(string token, string title, string body, Dictionary<string, string> data)
    {
        var serverKey = _config["Firebase:ServerKey"];
        if (string.IsNullOrEmpty(serverKey))
        {
            _logger.LogWarning("Firebase:ServerKey not set — skip push");
            return;
        }

        var payload = new
        {
            to = token,
            notification = new { title, body },
            data,
            priority = "high"
        };

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
        // FCM legacy API expects exactly: Authorization: key=<ServerKey>
        request.Headers.TryAddWithoutValidation("Authorization", "key=" + serverKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("FCM failed ({Status}): {Error}", response.StatusCode, err);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM send error");
        }
    }
}
