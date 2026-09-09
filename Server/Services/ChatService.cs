using Microsoft.EntityFrameworkCore;
using ICQ.Server.Data;
using ICQ.Server.Models;

namespace ICQ.Server.Services;

public class ChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatDto>> GetChatsForUserAsync(int userId)
    {
        var chats = await _db.ChatParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.Chat)
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        var result = new List<ChatDto>();
        foreach (var chat in chats)
        {
            var lastMsg = chat.Messages.FirstOrDefault();
            var unread = await _db.Messages.CountAsync(m => m.ChatId == chat.Id && m.SenderId != userId && !m.IsRead);
            result.Add(new ChatDto
            {
                Id = chat.Id,
                Title = chat.Title ?? string.Join(", ", chat.Participants.Where(p => p.UserId != userId).Select(p => p.User.Nickname)),
                Type = chat.Type,
                LastMessageAt = chat.LastMessageAt,
                LastMessagePreview = lastMsg == null ? null : (lastMsg.IsEncrypted ? "🔒 Encrypted" : Smileys.Expand(lastMsg.Content).Truncate(80)),
                Participants = chat.Participants.Select(p => ToUserDto(p.User)).ToList(),
                UnreadCount = unread
            });
        }
        return result;
    }

    public async Task<ChatDto?> GetOrCreatePrivateChatAsync(int userId, int otherUserId)
    {
        var existing = await _db.Chats
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Where(c => c.Type == "private")
            .Where(c => c.Participants.Count == 2
                        && c.Participants.Any(p => p.UserId == userId)
                        && c.Participants.Any(p => p.UserId == otherUserId))
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            return new ChatDto
            {
                Id = existing.Id,
                Title = existing.Participants.First(p => p.UserId != userId).User.Nickname,
                Type = existing.Type,
                LastMessageAt = existing.LastMessageAt,
                Participants = existing.Participants.Select(p => ToUserDto(p.User)).ToList()
            };
        }

        var chat = new Chat { Type = "private", CreatedAt = DateTime.UtcNow };
        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();

        _db.ChatParticipants.AddRange(
            new ChatParticipant { ChatId = chat.Id, UserId = userId },
            new ChatParticipant { ChatId = chat.Id, UserId = otherUserId }
        );
        await _db.SaveChangesAsync();

        var users = await _db.Users.Where(u => u.Id == userId || u.Id == otherUserId).ToListAsync();
        return new ChatDto
        {
            Id = chat.Id,
            Title = users.First(u => u.Id == otherUserId).Nickname,
            Type = "private",
            Participants = users.Select(ToUserDto).ToList()
        };
    }

    public async Task<MessageDto?> SendMessageAsync(int senderId, int chatId, string content, bool isEncrypted = false, string? encryptedPayload = null)
    {
        var isParticipant = await _db.ChatParticipants.AnyAsync(cp => cp.ChatId == chatId && cp.UserId == senderId);
        if (!isParticipant) return null;

        var msg = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Content = content ?? string.Empty,
            IsEncrypted = isEncrypted,
            EncryptedPayload = encryptedPayload,
            CreatedAt = DateTime.UtcNow
        };
        _db.Messages.Add(msg);

        var chat = await _db.Chats.FindAsync(chatId);
        if (chat != null) chat.LastMessageAt = msg.CreatedAt;

        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(senderId);
        return new MessageDto
        {
            Id = msg.Id,
            ChatId = msg.ChatId,
            SenderId = msg.SenderId,
            SenderNickname = sender?.Nickname ?? "",
            Content = msg.Content,
            IsEncrypted = msg.IsEncrypted,
            EncryptedPayload = msg.EncryptedPayload,
            CreatedAt = msg.CreatedAt,
            IsRead = false
        };
    }

    public async Task<List<MessageDto>> GetMessagesAsync(int userId, int chatId, int take = 50, int? beforeId = null)
    {
        var isParticipant = await _db.ChatParticipants.AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);
        if (!isParticipant) return new List<MessageDto>();

        var q = _db.Messages.Where(m => m.ChatId == chatId);
        if (beforeId.HasValue)
            q = q.Where(m => m.Id < beforeId.Value);

        var messages = await q.OrderByDescending(m => m.Id).Take(take)
            .Include(m => m.Sender)
            .ToListAsync();

        // mark as read
        foreach (var m in messages.Where(m => m.SenderId != userId && !m.IsRead))
        {
            m.IsRead = true;
        }
        await _db.SaveChangesAsync();

        return messages.OrderBy(m => m.Id).Select(m => new MessageDto
        {
            Id = m.Id,
            ChatId = m.ChatId,
            SenderId = m.SenderId,
            SenderNickname = m.Sender.Nickname,
            Content = m.Content,
            IsEncrypted = m.IsEncrypted,
            EncryptedPayload = m.EncryptedPayload,
            CreatedAt = m.CreatedAt,
            IsRead = m.IsRead,
            FileAttachmentId = m.FileAttachmentId
        }).ToList();
    }

    public async Task<List<int>> GetChatParticipantIdsAsync(int chatId)
    {
        return await _db.ChatParticipants.Where(cp => cp.ChatId == chatId).Select(cp => cp.UserId).ToListAsync();
    }

    public async Task SetOnlineStatusAsync(int userId, bool isOnline)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;
        user.IsOnline = isOnline;
        user.Status = isOnline ? "online" : "offline";
        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SetUserStatusAsync(int userId, string status)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;
        user.Status = status;
        if (status == "offline") user.IsOnline = false;
        else user.IsOnline = true;
        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<UserDto>> SearchUsersAsync(string query, int currentUserId, int take = 20)
    {
        query = query.Trim();
        if (string.IsNullOrEmpty(query)) return new();

        var q = _db.Users.AsQueryable().Where(u => u.Id != currentUserId);
        if (int.TryParse(query, out var uin))
            q = q.Where(u => u.Uin == uin || u.Nickname.Contains(query) || u.Email.Contains(query));
        else
            q = q.Where(u => u.Nickname.Contains(query) || u.Email.Contains(query));

        return await q.Take(take).Select(u => ToUserDto(u)).ToListAsync();
    }

    public async Task<List<UserDto>> GetContactsAsync(int userId)
    {
        return await _db.Contacts
            .Where(c => c.OwnerId == userId)
            .Include(c => c.ContactUser)
            .Select(c => ToUserDto(c.ContactUser))
            .ToListAsync();
    }

    public async Task<UserDto?> AddContactAsync(int ownerId, AddContactRequest req)
    {
        User? target = null;
        if (req.Uin.HasValue)
            target = await _db.Users.FirstOrDefaultAsync(u => u.Uin == req.Uin.Value);
        else if (!string.IsNullOrEmpty(req.Email))
            target = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        if (target == null || target.Id == ownerId) return null;

        var exists = await _db.Contacts.AnyAsync(c => c.OwnerId == ownerId && c.ContactUserId == target.Id);
        if (!exists)
        {
            _db.Contacts.Add(new Contact
            {
                OwnerId = ownerId,
                ContactUserId = target.Id,
                Nickname = req.Nickname ?? target.Nickname
            });
            await _db.SaveChangesAsync();
        }
        return ToUserDto(target);
    }

    private static UserDto ToUserDto(User u) => new()
    {
        Id = u.Id,
        Uin = u.Uin,
        Email = u.Email,
        Nickname = u.Nickname,
        Status = u.Status,
        StatusMessage = u.StatusMessage,
        IsOnline = u.IsOnline,
        LastSeenAt = u.LastSeenAt
    };
}

internal static class StringExt
{
    public static string Truncate(this string s, int max) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max] + "…");
}
