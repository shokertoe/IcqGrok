using ICQ.Server.Common;
using ICQ.Server.Data;
using ICQ.Server.Models;
using ICQ.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ICQ.Server.Hubs;

/// <summary>
/// Real-time transport adapter: presence, messaging fan-out, WebRTC signaling.
/// Business rules live in <see cref="IChatService"/>; connection map in <see cref="IPresenceTracker"/>.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IAuthService _authService;
    private readonly IPushService _pushService;
    private readonly IWebPushService _webPush;
    private readonly IPresenceTracker _presence;
    private readonly AppDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatService chatService,
        IAuthService authService,
        IPushService pushService,
        IWebPushService webPush,
        IPresenceTracker presence,
        AppDbContext db,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _authService = authService;
        _pushService = pushService;
        _webPush = webPush;
        _presence = presence;
        _db = db;
        _logger = logger;
    }

    private Guid? CurrentUserId =>
        _authService.GetUserIdFromPrincipal(Context.User!);

    private async Task SendToUserAsync(Guid userId, string method, params object?[] args)
    {
        foreach (var connId in _presence.GetConnections(userId))
            await Clients.Client(connId).SendAsync(method, args);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId;
        if (userId is null) { Context.Abort(); return; }

        _presence.AddConnection(userId.Value, Context.ConnectionId);
        await _chatService.UpdateUserStatusAsync(userId.Value, UserStatus.Online);
        await Clients.Others.SendAsync(HubEvents.UserStatusChanged, userId.Value, (int)UserStatus.Online);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId;
        if (userId is not null)
        {
            var wentOffline = _presence.RemoveConnection(userId.Value, Context.ConnectionId);
            if (wentOffline)
            {
                await _chatService.UpdateUserStatusAsync(userId.Value, UserStatus.Offline);
                await Clients.Others.SendAsync(HubEvents.UserStatusChanged, userId.Value, (int)UserStatus.Offline);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(Guid chatId)
    {
        var userId = CurrentUserId;
        if (userId is null) return;

        var isMember = await _db.ChatParticipants
            .AsNoTracking()
            .AnyAsync(p => p.ChatId == chatId && p.UserId == userId.Value);

        if (!isMember)
        {
            await Clients.Caller.SendAsync(HubEvents.Error, "Not a participant of this chat");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
    }

    public async Task LeaveChat(Guid chatId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());

    public async Task SendMessage(SendMessageRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return;

        var (message, error) = await _chatService.SendMessageAsync(userId.Value, request);
        if (error is not null || message is null)
        {
            await Clients.Caller.SendAsync(HubEvents.Error, error ?? "Failed to send");
            return;
        }

        var display = message.IsEncrypted
            ? message
            : message with { Text = SmileyPack.Expand(message.Text) };

        await Clients.OthersInGroup(request.ChatId.ToString()).SendAsync(HubEvents.ReceiveMessage, display);
        await Clients.Caller.SendAsync(HubEvents.ReceiveMessage, display);

        var participantIds = await _db.ChatParticipants
            .AsNoTracking()
            .Where(p => p.ChatId == request.ChatId && p.UserId != userId.Value)
            .Select(p => p.UserId)
            .ToListAsync();

        var preview = message.IsEncrypted ? "🔒 Encrypted message" : SmileyPack.Expand(message.Text);
        if (preview?.Length > 80) preview = preview[..80] + "…";

        foreach (var pid in participantIds)
        {
            if (_presence.IsOnline(pid)) continue;

            try
            {
                await _pushService.NotifyNewMessageAsync(pid, message, message.SenderNickname);
                await _webPush.NotifyAsync(pid, message.SenderNickname, preview ?? "New message",
                    new Dictionary<string, string>
                    {
                        ["chatId"] = message.ChatId.ToString(),
                        ["messageId"] = message.Id.ToString()
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push notify failed for {UserId}", pid);
            }
        }
    }

    public async Task Typing(Guid chatId, bool isTyping)
    {
        var userId = CurrentUserId;
        if (userId is null) return;

        var isMember = await _db.ChatParticipants
            .AsNoTracking()
            .AnyAsync(p => p.ChatId == chatId && p.UserId == userId.Value);
        if (!isMember) return;

        await Clients.OthersInGroup(chatId.ToString())
            .SendAsync(HubEvents.UserTyping, chatId, userId.Value, isTyping);
    }

    public async Task SetStatus(int status, string? statusMessage = null)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        if (!Enum.IsDefined(typeof(UserStatus), status))
        {
            await Clients.Caller.SendAsync(HubEvents.Error, "Invalid status value");
            return;
        }

        await _chatService.UpdateUserStatusAsync(userId.Value, (UserStatus)status, statusMessage);
        await Clients.Others.SendAsync(HubEvents.UserStatusChanged, userId.Value, status, statusMessage);
    }

    public Task<List<Guid>> GetOnlineUsers() =>
        Task.FromResult(_presence.OnlineUserIds.ToList());

    public async Task CallOffer(Guid targetUserId, string sdp, bool audioOnly, string? callerName)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        if (string.IsNullOrWhiteSpace(sdp) || sdp.Length > 256_000) return;
        await SendToUserAsync(targetUserId, HubEvents.CallOffer, userId.Value, sdp, audioOnly, callerName ?? "Caller");
    }

    public async Task CallAnswer(Guid callerId, string sdp)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        if (string.IsNullOrWhiteSpace(sdp) || sdp.Length > 256_000) return;
        await SendToUserAsync(callerId, HubEvents.CallAnswer, userId.Value, sdp);
    }

    public async Task IceCandidate(Guid peerId, string candidate)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > 16_384) return;
        await SendToUserAsync(peerId, HubEvents.IceCandidate, userId.Value, candidate);
    }

    public async Task CallHangup(Guid peerId)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await SendToUserAsync(peerId, HubEvents.CallHangup, userId.Value);
    }

    public async Task CallReject(Guid callerId)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await SendToUserAsync(callerId, HubEvents.CallReject, userId.Value);
    }
}
