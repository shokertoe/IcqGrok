using ICQ.Server.Models;

namespace ICQ.Server.Services.Abstractions;

public interface IChatService
{
    Task<(ContactDto? Contact, string? Error)> AddContactAsync(Guid ownerId, long targetUin);
    Task<(ContactDto? Contact, string? Error)> AcceptContactAsync(Guid userId, Guid contactId);
    Task<List<ContactDto>> GetContactsAsync(Guid userId);
    Task<(ChatDto? Chat, string? Error)> GetOrCreatePrivateChatAsync(Guid userId, Guid targetUserId);
    Task<(ChatDto? Chat, string? Error)> CreateGroupChatAsync(Guid ownerId, string title, List<Guid> participantIds);
    Task<List<ChatDto>> GetUserChatsAsync(Guid userId);
    Task<(MessageDto? Message, string? Error)> SendMessageAsync(Guid senderId, SendMessageRequest request);
    Task<List<MessageDto>> GetMessagesAsync(Guid userId, Guid chatId, int limit = 50, DateTime? before = null);
    Task<List<UserDto>> SearchUsersAsync(string query, Guid currentUserId, int limit = 20);
    Task UpdateUserStatusAsync(Guid userId, UserStatus status, string? statusMessage = null);
}
