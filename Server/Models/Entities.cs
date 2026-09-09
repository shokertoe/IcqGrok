using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ICQ.Server.Models;

public class User
{
    public int Id { get; set; }
    public int Uin { get; set; }
    [MaxLength(128)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(64)]
    public string Nickname { get; set; } = string.Empty;
    [MaxLength(32)]
    public string Status { get; set; } = "offline";
    [MaxLength(256)]
    public string? StatusMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
    public bool IsOnline { get; set; }

    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}

public class Chat
{
    public int Id { get; set; }
    [MaxLength(128)]
    public string? Title { get; set; }
    [MaxLength(16)]
    public string Type { get; set; } = "private"; // private | group
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }

    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class ChatParticipant
{
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; }
}

public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    [MaxLength(8000)]
    public string Content { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    [MaxLength(16000)]
    public string? EncryptedPayload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public int? FileAttachmentId { get; set; }
}

public class Contact
{
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public int ContactUserId { get; set; }
    public User ContactUser { get; set; } = null!;
    [MaxLength(64)]
    public string? Nickname { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
}

public class UserKeyBundle
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(256)]
    public string IdentityKeyPublic { get; set; } = string.Empty;
    [MaxLength(256)]
    public string SignedPreKeyPublic { get; set; } = string.Empty;
    [MaxLength(512)]
    public string SignedPreKeySignature { get; set; } = string.Empty;
    public int SignedPreKeyId { get; set; }
    [MaxLength(8000)]
    public string? OneTimePreKeysJson { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PushSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(1024)]
    public string Endpoint { get; set; } = string.Empty;
    [MaxLength(256)]
    public string P256dh { get; set; } = string.Empty;
    [MaxLength(128)]
    public string Auth { get; set; } = string.Empty;
    [MaxLength(32)]
    public string Platform { get; set; } = "web"; // web | android | ios
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FileAttachment
{
    public int Id { get; set; }
    public int? MessageId { get; set; }
    public Message? Message { get; set; }
    [MaxLength(256)]
    public string FileName { get; set; } = string.Empty;
    [MaxLength(128)]
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    [MaxLength(512)]
    public string StoragePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int UploaderId { get; set; }
}
