using ICQ.Server.Data;
using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ICQ.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly AuthService _authService;
    private readonly PushService _pushService;
    private readonly WebPushService _webPush;
    private readonly AppDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    private static readonly Dictionary<Guid, HashSet<string>> OnlineUsers = new();
    private static readonly object Lock = new();

    public ChatHub(
        ChatService chatService,
        AuthService authService,
        PushService pushService,
        WebPushService webPush,
        AppDbContext db,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _authService = authService;
        _pushService = pushService;
        _webPush = webPush;
        _db = db;
        _logger = logger;
    }

    private Guid? CurrentUserId =>
        _authService.GetUserIdFromPrincipal(Context.User!);

    private static IReadOnlyList<string> ConnectionsOf(Guid userId)
    {
        lock (Lock)
        {
            return OnlineUsers.TryGetValue(userId, out var set)
                ? set.ToList()
                : Array.Empty<string>();
        }
    }

    private async Task SendToUserAsync(Guid userId, string method, params object?[] args)
    {
        foreach (var connId in ConnectionsOf(userId))
            await Clients.Client(connId).SendAsync(method, args);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId;
        if (userId is null) { Context.Abort(); return; }

        lock (Lock)
        {
            if (!OnlineUsers.ContainsKey(userId.Value))
                OnlineUsers[userId.Value] = new HashSet<string>();
            OnlineUsers[userId.Value].Add(Context.ConnectionId);
        }

        await _chatService.UpdateUserStatusAsync(userId.Value, UserStatus.Online);
        await Clients.Others.SendAsync("UserStatusChanged", userId.Value, (int)UserStatus.Online);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId;
        if (userId is not null)
        {
            bool wentOffline = false;
            lock (Lock)
            {
                if (OnlineUsers.TryGetValue(userId.Value, out var connections))
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                    {
                        OnlineUsers.Remove(userId.Value);
                        wentOffline = true;
                    }
                }
            }
            if (wentOffline)
            {
                await _chatService.UpdateUserStatusAsync(userId.Value, UserStatus.Offline);
                await Clients.Others.SendAsync("UserStatusChanged", userId.Value, (int)UserStatus.Offline);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(Guid chatId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

    public async Task LeaveChat(Guid chatId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());

    public async Task SendMessage(SendMessageRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return;

        var (message, error) = await _chatService.SendMessageAsync(userId.Value, request);
        if (error is not null || message is null)
        {
            await Clients.Caller.SendAsync("Error", error ?? "Failed to send");
            return;
        }

        var display = message.IsEncrypted
            ? message
            : message with { Text = SmileyPack.Expand(message.Text) };
        await Clients.Group(request.ChatId.ToString()).SendAsync("ReceiveMessage", display);

        var participantIds = await _db.ChatParticipants
            .Where(p => p.ChatId == request.ChatId && p.UserId != userId.Value)
            .Select(p => p.UserId)
            .ToListAsync();

        List<Guid> online;
        lock (Lock) { online = OnlineUsers.Keys.ToList(); }

        var preview = message.IsEncrypted ? "🔒 Encrypted message" : SmileyPack.Expand(message.Text);
        if (preview?.Length > 80) preview = preview[..80] + "…";

        foreach (var pid in participantIds)
        {
            if (!online.Contains(pid))
            {
                await _pushService.NotifyNewMessageAsync(pid, message, message.SenderNickname);
                await _webPush.NotifyAsync(pid, message.SenderNickname, preview ?? "New message",
                    new Dictionary<string, string>
                    {
                        ["chatId"] = message.ChatId.ToString(),
                        ["messageId"] = message.Id.ToString()
                    });
            }
        }
    }

    public async Task Typing(Guid chatId, bool isTyping)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await Clients.OthersInGroup(chatId.ToString())
            .SendAsync("UserTyping", chatId, userId.Value, isTyping);
    }

    public async Task SetStatus(int status, string? statusMessage = null)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await _chatService.UpdateUserStatusAsync(userId.Value, (UserStatus)status, statusMessage);
        await Clients.Others.SendAsync("UserStatusChanged", userId.Value, status, statusMessage);
    }

    public Task<List<Guid>> GetOnlineUsers()
    {
        lock (Lock) return Task.FromResult(OnlineUsers.Keys.ToList());
    }

    // ─── WebRTC signaling (targeted to peer connections) ────────

    /// <summary>Start call: audioOnly true = voice, false = video</summary>
    public async Task CallOffer(Guid targetUserId, string sdp, bool audioOnly, string? callerName)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await SendToUserAsync(targetUserId, "CallOffer", userId.Value, sdp, audioOnly, callerName ?? "Caller");
    }

    public async Task CallAnswer(Guid callerId, string sdp)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await SendToUserAsync(callerId, "CallAnswer", userId.Value, sdp);
    }

    public async Task IceCandidate(Guid peerId, string candidate)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await SendToUserAsync(peerId, "IceCandidate", userId.Value, candidate);
    }

    public async Task CallHangup(Guid peerId)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await SendToUserAsync(peerId, "CallHangup", userId.Value);
    }

    public async Task CallReject(Guid callerId)
    {
        var userId = CurrentUserId;
        if (userId is null) return;
        await SendToUserAsync(callerId, "CallReject", userId.Value);
    }
}
