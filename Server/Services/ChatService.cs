using ICQ.Server.Data;
using ICQ.Server.Models;
using ICQ.Server.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ICQ.Server.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ChatService> _logger;

    public ChatService(AppDbContext db, ILogger<ChatService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(ContactDto? Contact, string? Error)> AddContactAsync(Guid ownerId, long targetUin)
    {
        var target = await _db.Users.FirstOrDefaultAsync(u => u.Uin == targetUin);
        if (target is null)
            return (null, "User not found");

        if (target.Id == ownerId)
            return (null, "Cannot add yourself");

        if (await _db.Contacts.AnyAsync(c => c.OwnerId == ownerId && c.ContactUserId == target.Id))
            return (null, "Already in contacts");

        var contact = new Contact
        {
            OwnerId = ownerId,
            ContactUserId = target.Id,
            Status = ContactStatus.Pending
        };

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();

        await _db.Entry(contact).Reference(c => c.ContactUser).LoadAsync();

        return (MapContact(contact), null);
    }

    public async Task<(ContactDto? Contact, string? Error)> AcceptContactAsync(Guid userId, Guid contactId)
    {
        var contact = await _db.Contacts
            .Include(c => c.Owner)
            .Include(c => c.ContactUser)
            .FirstOrDefaultAsync(c => c.Id == contactId && c.ContactUserId == userId);

        if (contact is null)
            return (null, "Contact request not found");

        contact.Status = ContactStatus.Accepted;

        if (!await _db.Contacts.AnyAsync(c => c.OwnerId == userId && c.ContactUserId == contact.OwnerId))
        {
            _db.Contacts.Add(new Contact
            {
                OwnerId = userId,
                ContactUserId = contact.OwnerId,
                Status = ContactStatus.Accepted
            });
        }
        else
        {
            var reverse = await _db.Contacts
                .FirstAsync(c => c.OwnerId == userId && c.ContactUserId == contact.OwnerId);
            reverse.Status = ContactStatus.Accepted;
        }

        await _db.SaveChangesAsync();
        return (MapContact(contact), null);
    }

    public async Task<List<ContactDto>> GetContactsAsync(Guid userId)
    {
        var contacts = await _db.Contacts
            .Include(c => c.ContactUser)
            .Where(c => c.OwnerId == userId && c.Status != ContactStatus.Blocked)
            .OrderBy(c => c.GroupOrder)
            .ThenBy(c => c.ContactUser.Nickname)
            .ToListAsync();

        return contacts.Select(MapContact).ToList();
    }

    public async Task<(ChatDto? Chat, string? Error)> GetOrCreatePrivateChatAsync(Guid userId, Guid targetUserId)
    {
        if (userId == targetUserId)
            return (null, "Cannot chat with yourself");

        var existing = await _db.Chats
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .Where(c => c.Type == ChatType.Private)
            .Where(c => c.Participants.Count == 2
                        && c.Participants.Any(p => p.UserId == userId)
                        && c.Participants.Any(p => p.UserId == targetUserId))
            .FirstOrDefaultAsync();

        if (existing is not null)
            return (await MapChatAsync(existing, userId), null);

        var target = await _db.Users.FindAsync(targetUserId);
        if (target is null)
            return (null, "User not found");

        var chat = new Chat { Type = ChatType.Private };
        _db.Chats.Add(chat);

        _db.ChatParticipants.AddRange(
            new ChatParticipant { Chat = chat, UserId = userId, Role = ParticipantRole.Member },
            new ChatParticipant { Chat = chat, UserId = targetUserId, Role = ParticipantRole.Member }
        );

        await _db.SaveChangesAsync();

        await _db.Entry(chat).Collection(c => c.Participants).Query()
            .Include(p => p.User).LoadAsync();

        return (await MapChatAsync(chat, userId), null);
    }

    public async Task<(ChatDto? Chat, string? Error)> CreateGroupChatAsync(Guid ownerId, string title, List<Guid> participantIds)
    {
        var uniqueIds = participantIds.Distinct().Where(id => id != ownerId).ToList();
        if (uniqueIds.Count == 0)
            return (null, "At least one other participant required");

        var usersExist = await _db.Users.CountAsync(u => uniqueIds.Contains(u.Id));
        if (usersExist != uniqueIds.Count)
            return (null, "One or more users not found");

        var chat = new Chat
        {
            Type = ChatType.Group,
            Title = title.Trim()
        };
        _db.Chats.Add(chat);

        _db.ChatParticipants.Add(new ChatParticipant
        {
            Chat = chat,
            UserId = ownerId,
            Role = ParticipantRole.Owner
        });

        foreach (var pid in uniqueIds)
        {
            _db.ChatParticipants.Add(new ChatParticipant
            {
                Chat = chat,
                UserId = pid,
                Role = ParticipantRole.Member
            });
        }

        await _db.SaveChangesAsync();

        await _db.Entry(chat).Collection(c => c.Participants).Query()
            .Include(p => p.User).LoadAsync();

        return (await MapChatAsync(chat, ownerId), null);
    }

    public async Task<List<ChatDto>> GetUserChatsAsync(Guid userId)
    {
        var chats = await _db.Chats
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        var result = new List<ChatDto>();
        foreach (var chat in chats)
            result.Add(await MapChatAsync(chat, userId));

        return result;
    }

    public async Task<(MessageDto? Message, string? Error)> SendMessageAsync(Guid senderId, SendMessageRequest request)
    {
        var isParticipant = await _db.ChatParticipants
            .AnyAsync(p => p.ChatId == request.ChatId && p.UserId == senderId);

        if (!isParticipant)
            return (null, "Not a participant of this chat");

        if (!string.IsNullOrEmpty(request.ClientMessageId))
        {
            var existing = await _db.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.ClientMessageId == request.ClientMessageId && m.SenderId == senderId);
            if (existing is not null)
                return (MapMessage(existing), null);
        }

        var message = new Message
        {
            ChatId = request.ChatId,
            SenderId = senderId,
            Type = request.Type,
            Text = request.Text,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentName = request.AttachmentName,
            AttachmentSize = request.AttachmentSize,
            ClientMessageId = request.ClientMessageId,
            IsEncrypted = request.IsEncrypted
        };

        _db.Messages.Add(message);

        var chat = await _db.Chats.FindAsync(request.ChatId);
        if (chat is not null)
            chat.LastMessageAt = message.SentAt;

        await _db.SaveChangesAsync();

        await _db.Entry(message).Reference(m => m.Sender).LoadAsync();

        return (MapMessage(message), null);
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid userId, Guid chatId, int limit = 50, DateTime? before = null)
    {
        var isParticipant = await _db.ChatParticipants
            .AnyAsync(p => p.ChatId == chatId && p.UserId == userId);
        if (!isParticipant)
            return new List<MessageDto>();

        var query = _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ChatId == chatId && !m.IsDeleted);

        if (before.HasValue)
            query = query.Where(m => m.SentAt < before.Value);

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();

        var participant = await _db.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);
        if (participant is not null)
        {
            participant.LastReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return messages.OrderBy(m => m.SentAt).Select(MapMessage).ToList();
    }

    public async Task<List<UserDto>> SearchUsersAsync(string query, Guid currentUserId, int limit = 20)
    {
        query = query.Trim().ToLower();
        if (string.IsNullOrEmpty(query))
            return new List<UserDto>();

        long? uinQuery = long.TryParse(query, out var u) ? u : null;

        var users = await _db.Users
            .Where(u => u.Id != currentUserId &&
                        (u.Nickname.ToLower().Contains(query) ||
                         (uinQuery.HasValue && u.Uin == uinQuery.Value) ||
                         (u.FirstName != null && u.FirstName.ToLower().Contains(query)) ||
                         (u.LastName != null && u.LastName.ToLower().Contains(query))))
            .OrderBy(u => u.Nickname)
            .Take(limit)
            .ToListAsync();

        return users.Select(AuthService.MapToDto).ToList();
    }

    public async Task UpdateUserStatusAsync(Guid userId, UserStatus status, string? statusMessage = null)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return;

        user.Status = status;
        if (statusMessage is not null)
            user.StatusMessage = statusMessage;
        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static ContactDto MapContact(Contact c) => new(
        c.Id,
        AuthService.MapToDto(c.ContactUser),
        c.NicknameOverride,
        c.Status,
        c.AddedAt
    );

    private async Task<ChatDto> MapChatAsync(Chat chat, Guid currentUserId)
    {
        var participants = chat.Participants
            .Select(p => AuthService.MapToDto(p.User))
            .ToList();

        MessageDto? lastMessage = null;
        if (chat.Messages.Any())
        {
            var msg = chat.Messages.First();
            if (msg.Sender is null)
                await _db.Entry(msg).Reference(m => m.Sender).LoadAsync();
            lastMessage = MapMessage(msg);
        }

        var participant = chat.Participants.FirstOrDefault(p => p.UserId == currentUserId);
        var unread = 0;
        if (participant?.LastReadAt is not null)
        {
            unread = await _db.Messages.CountAsync(m =>
                m.ChatId == chat.Id &&
                m.SentAt > participant.LastReadAt &&
                m.SenderId != currentUserId &&
                !m.IsDeleted);
        }
        else if (participant is not null)
        {
            unread = await _db.Messages.CountAsync(m =>
                m.ChatId == chat.Id &&
                m.SenderId != currentUserId &&
                !m.IsDeleted);
        }

        string? title = chat.Title;
        if (chat.Type == ChatType.Private && string.IsNullOrEmpty(title))
        {
            var other = chat.Participants.FirstOrDefault(p => p.UserId != currentUserId);
            title = other?.User.Nickname;
        }

        return new ChatDto(
            chat.Id,
            chat.Type,
            title,
            chat.CreatedAt,
            chat.LastMessageAt,
            participants,
            lastMessage,
            unread
        );
    }

    private static MessageDto MapMessage(Message m) => new(
        m.Id,
        m.ChatId,
        m.SenderId,
        m.Sender?.Nickname ?? "?",
        m.Type,
        m.Text,
        m.AttachmentUrl,
        m.AttachmentName,
        m.AttachmentSize,
        m.SentAt,
        m.EditedAt,
        m.IsDeleted,
        m.ClientMessageId,
        m.IsEncrypted
    );
}
