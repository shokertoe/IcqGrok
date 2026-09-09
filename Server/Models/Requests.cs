using System.ComponentModel.DataAnnotations;

namespace ICQ.Server.Models;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(128)]
    public string Email { get; set; } = string.Empty;
    [Required, MinLength(6), MaxLength(64)]
    public string Password { get; set; } = string.Empty;
    [Required, MaxLength(64)]
    public string Nickname { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string EmailOrUin { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public int Id { get; set; }
    public int Uin { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public class CreateChatRequest
{
    public int? OtherUserId { get; set; }
    public string? Title { get; set; }
    public List<int>? ParticipantIds { get; set; }
}

public class SendMessageRequest
{
    [Required]
    public int ChatId { get; set; }
    [MaxLength(8000)]
    public string Content { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    [MaxLength(16000)]
    public string? EncryptedPayload { get; set; }
}

public class MessageDto
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string SenderNickname { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public string? EncryptedPayload { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public int? FileAttachmentId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
}

public class ChatDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public List<UserDto> Participants { get; set; } = new();
    public int UnreadCount { get; set; }
}

public class AddContactRequest
{
    public int? Uin { get; set; }
    public string? Email { get; set; }
    public string? Nickname { get; set; }
}

public class UpdateStatusRequest
{
    [MaxLength(32)]
    public string Status { get; set; } = "online";
    [MaxLength(256)]
    public string? StatusMessage { get; set; }
}

public class UploadKeyBundleRequest
{
    [Required]
    public string IdentityKeyPublic { get; set; } = string.Empty;
    [Required]
    public string SignedPreKeyPublic { get; set; } = string.Empty;
    [Required]
    public string SignedPreKeySignature { get; set; } = string.Empty;
    public int SignedPreKeyId { get; set; }
    public string? OneTimePreKeysJson { get; set; }
}

public class KeyBundleDto
{
    public int UserId { get; set; }
    public string IdentityKeyPublic { get; set; } = string.Empty;
    public string SignedPreKeyPublic { get; set; } = string.Empty;
    public string SignedPreKeySignature { get; set; } = string.Empty;
    public int SignedPreKeyId { get; set; }
    public string? OneTimePreKey { get; set; }
}

public class WebPushSubscribeRequest
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
    [Required]
    public string P256dh { get; set; } = string.Empty;
    [Required]
    public string Auth { get; set; } = string.Empty;
    public string Platform { get; set; } = "web";
}
