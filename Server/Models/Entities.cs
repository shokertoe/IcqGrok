using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ICQ.Server.Models;

// ═══════════════════════════════════════════════════════════════
// USER
// ═══════════════════════════════════════════════════════════════

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>UIN-like numeric ID (auto-increment style, but GUID primary)</summary>
    [Required]
    public long Uin { get; set; }

    [Required, MaxLength(64)]
    public string Nickname { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(128)]
    public string? FirstName { get; set; }

    [MaxLength(128)]
    public string? LastName { get; set; }

    [MaxLength(512)]
    public string? StatusMessage { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Offline;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }

    // Navigation
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
}

public enum UserStatus
{
    Offline = 0,
    Online = 1,
    Away = 2,
    Busy = 3,
    Invisible = 4
}

// ═══════════════════════════════════════════════════════════════
// CONTACT (Buddy List)
// ═══════════════════════════════════════════════════════════════

public class Contact
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public Guid ContactUserId { get; set; }
    public User ContactUser { get; set; } = null!;

    [MaxLength(64)]
    public string? NicknameOverride { get; set; }

    public ContactStatus Status { get; set; } = ContactStatus.Pending;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public int GroupOrder { get; set; } = 0;
}

public enum ContactStatus
{
    Pending = 0,
    Accepted = 1,
    Blocked = 2,
    Denied = 3
}

// ═══════════════════════════════════════════════════════════════
// CHAT
// ═══════════════════════════════════════════════════════════════

public class Chat
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public ChatType Type { get; set; } = ChatType.Private;

    [MaxLength(128)]
    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }

    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public enum ChatType
{
    Private = 0,
    Group = 1,
    Channel = 2
}

public class ChatParticipant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ParticipantRole Role { get; set; } = ParticipantRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }

    public bool IsMuted { get; set; } = false;
}

public enum ParticipantRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}

// ═══════════════════════════════════════════════════════════════
// MESSAGE
// ═══════════════════════════════════════════════════════════════

public class Message
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public MessageType Type { get; set; } = MessageType.Text;

    [MaxLength(8192)]
    public string? Text { get; set; }

    [MaxLength(1024)]
    public string? AttachmentUrl { get; set; }

    [MaxLength(256)]
    public string? AttachmentName { get; set; }

    public long? AttachmentSize { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    /// <summary>True if Text is ciphertext (E2E)</summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>Client-generated ID for optimistic UI and deduplication</summary>
    [MaxLength(64)]
    public string? ClientMessageId { get; set; }
}

public enum MessageType
{
    Text = 0,
    Image = 1,
    File = 2,
    System = 3,
    Sticker = 4
}

// ═══════════════════════════════════════════════════════════════
// REFRESH TOKEN
// ═══════════════════════════════════════════════════════════════

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
}

// ═══════════════════════════════════════════════════════════════
// DEVICE TOKEN (Push)
// ═══════════════════════════════════════════════════════════════

public class DeviceToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    /// <summary>ios | android | macos</summary>
    [MaxLength(32)]
    public string Platform { get; set; } = "unknown";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════
// WEB PUSH SUBSCRIPTION (PWA / iOS home screen)
// ═══════════════════════════════════════════════════════════════

public class WebPushSubscription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(2048)]
    public string Endpoint { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string P256dh { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Auth { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════
// E2E KEY BUNDLE (public keys only — server never sees private keys)
// ═══════════════════════════════════════════════════════════════

public class UserKeyBundle
{
    [Key]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Base64 SPKI public key (ECDH P-256)</summary>
    [Required, MaxLength(512)]
    public string IdentityPublicKey { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
