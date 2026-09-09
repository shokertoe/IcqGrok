using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ICQ.Server.Models;
using ICQ.Server.Services;

namespace ICQ.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly PushService _pushService;
    private readonly ILogger<ChatHub> _logger;

    // connectionId -> userId
    private static readonly Dictionary<string, int> ConnectionUsers = new();
    // userId -> set of connectionIds
    private static readonly Dictionary<int, HashSet<string>> UserConnections = new();

    public ChatHub(ChatService chatService, PushService pushService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _pushService = pushService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == null) { Context.Abort(); return; }

        lock (UserConnections)
        {
            ConnectionUsers[Context.ConnectionId] = userId.Value;
            if (!UserConnections.TryGetValue(userId.Value, out var set))
            {
                set = new HashSet<string>();
                UserConnections[userId.Value] = set;
            }
            set.Add(Context.ConnectionId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await _chatService.SetOnlineStatusAsync(userId.Value, true);
        await Clients.Others.SendAsync("UserStatusChanged", userId.Value, "online");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        int? userId = null;
        lock (UserConnections)
        {
            if (ConnectionUsers.TryGetValue(Context.ConnectionId, out var uid))
            {
                userId = uid;
                ConnectionUsers.Remove(Context.ConnectionId);
                if (UserConnections.TryGetValue(uid, out var set))
                {
                    set.Remove(Context.ConnectionId);
                    if (set.Count == 0)
                    {
                        UserConnections.Remove(uid);
                    }
                }
            }
        }

        if (userId != null)
        {
            var stillOnline = false;
            lock (UserConnections)
            {
                stillOnline = UserConnections.ContainsKey(userId.Value);
            }
            if (!stillOnline)
            {
                await _chatService.SetOnlineStatusAsync(userId.Value, false);
                await Clients.Others.SendAsync("UserStatusChanged", userId.Value, "offline");
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(int chatId, string content, bool isEncrypted = false, string? encryptedPayload = null)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var msg = await _chatService.SendMessageAsync(userId.Value, chatId, content, isEncrypted, encryptedPayload);
        if (msg == null) return;

        var participants = await _chatService.GetChatParticipantIdsAsync(chatId);
        foreach (var pid in participants)
        {
            await Clients.Group($"user_{pid}").SendAsync("ReceiveMessage", msg);
            if (pid != userId.Value)
            {
                await _pushService.SendMessagePushAsync(pid, msg);
            }
        }
    }

    public async Task Typing(int chatId, bool isTyping)
    {
        var userId = GetUserId();
        if (userId == null) return;
        var participants = await _chatService.GetChatParticipantIdsAsync(chatId);
        foreach (var pid in participants.Where(p => p != userId.Value))
        {
            await Clients.Group($"user_{pid}").SendAsync("UserTyping", chatId, userId.Value, isTyping);
        }
    }

    public async Task SetStatus(string status)
    {
        var userId = GetUserId();
        if (userId == null) return;
        await _chatService.SetUserStatusAsync(userId.Value, status);
        await Clients.Others.SendAsync("UserStatusChanged", userId.Value, status);
    }

    // WebRTC signaling - targeted to specific user connections
    public async Task CallOffer(int targetUserId, string sdp, string callType)
    {
        var userId = GetUserId();
        if (userId == null) return;
        await Clients.Group($"user_{targetUserId}").SendAsync("CallOffer", userId.Value, sdp, callType, Context.ConnectionId);
    }

    public async Task CallAnswer(int targetUserId, string sdp, string targetConnectionId)
    {
        var userId = GetUserId();
        if (userId == null) return;
        if (!string.IsNullOrEmpty(targetConnectionId))
            await Clients.Client(targetConnectionId).SendAsync("CallAnswer", userId.Value, sdp, Context.ConnectionId);
        else
            await Clients.Group($"user_{targetUserId}").SendAsync("CallAnswer", userId.Value, sdp, Context.ConnectionId);
    }

    public async Task IceCandidate(int targetUserId, string candidate, string? targetConnectionId)
    {
        var userId = GetUserId();
        if (userId == null) return;
        if (!string.IsNullOrEmpty(targetConnectionId))
            await Clients.Client(targetConnectionId).SendAsync("IceCandidate", userId.Value, candidate, Context.ConnectionId);
        else
            await Clients.Group($"user_{targetUserId}").SendAsync("IceCandidate", userId.Value, candidate, Context.ConnectionId);
    }

    public async Task Hangup(int targetUserId, string? targetConnectionId)
    {
        var userId = GetUserId();
        if (userId == null) return;
        if (!string.IsNullOrEmpty(targetConnectionId))
            await Clients.Client(targetConnectionId).SendAsync("Hangup", userId.Value);
        else
            await Clients.Group($"user_{targetUserId}").SendAsync("Hangup", userId.Value);
    }

    public async Task RejectCall(int targetUserId, string? targetConnectionId)
    {
        var userId = GetUserId();
        if (userId == null) return;
        if (!string.IsNullOrEmpty(targetConnectionId))
            await Clients.Client(targetConnectionId).SendAsync("CallRejected", userId.Value);
        else
            await Clients.Group($"user_{targetUserId}").SendAsync("CallRejected", userId.Value);
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User?.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public static IReadOnlyCollection<string> GetConnectionsForUser(int userId)
    {
        lock (UserConnections)
        {
            return UserConnections.TryGetValue(userId, out var set) ? set.ToList() : Array.Empty<string>();
        }
    }
}
