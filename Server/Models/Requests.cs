using System.ComponentModel.DataAnnotations;

namespace ICQ.Server.Models;

// ─── Auth ───────────────────────────────────────────────────────

/// <summary>Регистрация нового пользователя.</summary>
public record RegisterRequest(
    [Required(ErrorMessage = "Никнейм обязателен")]
    [MinLength(3, ErrorMessage = "Никнейм минимум 3 символа")]
    [MaxLength(64, ErrorMessage = "Никнейм максимум 64 символа")]
    [RegularExpression(@"^[a-zA-Z0-9_\.\-]+$", ErrorMessage = "Никнейм: только латиница, цифры, _ . -")]
    string Nickname,

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль минимум 6 символов")]
    [MaxLength(128, ErrorMessage = "Пароль максимум 128 символов")]
    string Password,

    [EmailAddress(ErrorMessage = "Некорректный email")]
    [MaxLength(256, ErrorMessage = "Email максимум 256 символов")]
    string? Email = null,

    [MaxLength(128, ErrorMessage = "Имя максимум 128 символов")]
    string? FirstName = null,

    [MaxLength(128, ErrorMessage = "Фамилия максимум 128 символов")]
    string? LastName = null
);

/// <summary>Вход в систему.</summary>
public record LoginRequest(
    [Required(ErrorMessage = "Логин обязателен")]
    [MaxLength(256, ErrorMessage = "Логин слишком длинный")]
    string NicknameOrEmail,

    [Required(ErrorMessage = "Пароль обязателен")]
    [MaxLength(128, ErrorMessage = "Пароль слишком длинный")]
    string Password
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

/// <summary>Обновление / отзыв refresh-токена.</summary>
public record RefreshRequest(
    [Required(ErrorMessage = "Refresh-токен обязателен")]
    [MaxLength(512, ErrorMessage = "Некорректный refresh-токен")]
    string RefreshToken
);

// ─── User DTOs ──────────────────────────────────────────────────

public record UserDto(
    Guid Id,
    long Uin,
    string Nickname,
    string? Email,
    string? FirstName,
    string? LastName,
    string? StatusMessage,
    UserStatus Status,
    DateTime? LastSeenAt,
    string? AvatarUrl
);

/// <summary>Обновление профиля.</summary>
public record UpdateProfileRequest(
    [MaxLength(128, ErrorMessage = "Имя максимум 128 символов")]
    string? FirstName,

    [MaxLength(128, ErrorMessage = "Фамилия максимум 128 символов")]
    string? LastName,

    [MaxLength(512, ErrorMessage = "Статус максимум 512 символов")]
    string? StatusMessage,

    [MaxLength(512, ErrorMessage = "URL аватара максимум 512 символов")]
    [Url(ErrorMessage = "Некорректный URL аватара")]
    string? AvatarUrl
);

/// <summary>Смена статуса присутствия.</summary>
public record UpdateStatusRequest(
    [Required(ErrorMessage = "Статус обязателен")]
    [EnumDataType(typeof(UserStatus), ErrorMessage = "Неизвестный статус")]
    UserStatus Status,

    [MaxLength(512, ErrorMessage = "Статусное сообщение максимум 512 символов")]
    string? StatusMessage = null
);

// ─── Contacts ───────────────────────────────────────────────────

public record ContactDto(
    Guid Id,
    UserDto User,
    string? NicknameOverride,
    ContactStatus Status,
    DateTime AddedAt
);

/// <summary>Добавить контакт по UIN.</summary>
public record AddContactRequest(
    [Required(ErrorMessage = "UIN обязателен")]
    [Range(1, long.MaxValue, ErrorMessage = "UIN должен быть положительным числом")]
    long TargetUin
);

public record ContactActionRequest(
    [Required(ErrorMessage = "Id контакта обязателен")]
    Guid ContactId
);

// ─── Chats ──────────────────────────────────────────────────────

public record ChatDto(
    Guid Id,
    ChatType Type,
    string? Title,
    DateTime CreatedAt,
    DateTime? LastMessageAt,
    List<UserDto> Participants,
    MessageDto? LastMessage,
    int UnreadCount
);

/// <summary>Создать / открыть личный чат.</summary>
public record CreatePrivateChatRequest(
    [Required(ErrorMessage = "Id собеседника обязателен")]
    Guid TargetUserId
);

/// <summary>Создать групповой чат.</summary>
public record CreateGroupChatRequest(
    [Required(ErrorMessage = "Название группы обязательно")]
    [MinLength(1, ErrorMessage = "Название не может быть пустым")]
    [MaxLength(128, ErrorMessage = "Название максимум 128 символов")]
    string Title,

    [Required(ErrorMessage = "Список участников обязателен")]
    [MinLength(1, ErrorMessage = "Нужен хотя бы один участник")]
    List<Guid> ParticipantIds
);

// ─── Messages ───────────────────────────────────────────────────

public record MessageDto(
    Guid Id,
    Guid ChatId,
    Guid SenderId,
    string SenderNickname,
    MessageType Type,
    string? Text,
    string? AttachmentUrl,
    string? AttachmentName,
    long? AttachmentSize,
    DateTime SentAt,
    DateTime? EditedAt,
    bool IsDeleted,
    string? ClientMessageId,
    bool IsEncrypted = false
);

/// <summary>Отправка сообщения (текст / файл / E2E ciphertext).</summary>
public record SendMessageRequest(
    [Required(ErrorMessage = "Id чата обязателен")]
    Guid ChatId,

    [Required(ErrorMessage = "Текст сообщения обязателен")]
    [MaxLength(16384, ErrorMessage = "Сообщение максимум 16384 символов")]
    string Text,

    MessageType Type = MessageType.Text,

    [MaxLength(1024, ErrorMessage = "URL вложения слишком длинный")]
    string? AttachmentUrl = null,

    [MaxLength(256, ErrorMessage = "Имя файла максимум 256 символов")]
    string? AttachmentName = null,

    [Range(0, 26_214_400, ErrorMessage = "Размер файла от 0 до 25 МБ")]
    long? AttachmentSize = null,

    [MaxLength(64, ErrorMessage = "ClientMessageId максимум 64 символа")]
    string? ClientMessageId = null,

    bool IsEncrypted = false
);

/// <summary>Публичный ECDH-ключ для E2E.</summary>
public record UploadKeyBundleRequest(
    [Required(ErrorMessage = "Публичный ключ обязателен")]
    [MinLength(16, ErrorMessage = "Ключ слишком короткий")]
    [MaxLength(512, ErrorMessage = "Ключ максимум 512 символов")]
    string IdentityPublicKey
);

public record KeyBundleDto(
    Guid UserId,
    string IdentityPublicKey,
    DateTime UpdatedAt
);

/// <summary>Редактирование своего сообщения.</summary>
public record EditMessageRequest(
    [Required(ErrorMessage = "Id сообщения обязателен")]
    Guid MessageId,

    [Required(ErrorMessage = "Новый текст обязателен")]
    [MinLength(1, ErrorMessage = "Текст не может быть пустым")]
    [MaxLength(8192, ErrorMessage = "Текст максимум 8192 символа")]
    string NewText
);

// ─── Search ─────────────────────────────────────────────────────

public record SearchUsersRequest(
    [Required(ErrorMessage = "Строка поиска обязательна")]
    [MinLength(1, ErrorMessage = "Введите хотя бы 1 символ")]
    [MaxLength(64, ErrorMessage = "Запрос максимум 64 символа")]
    string Query,

    [Range(1, 100, ErrorMessage = "Limit от 1 до 100")]
    int Limit = 20
);

public record SearchUsersResponse(
    List<UserDto> Users
);

// ─── Push ───────────────────────────────────────────────────────

/// <summary>Регистрация device token (FCM / APNs).</summary>
public record RegisterDeviceRequest(
    [Required(ErrorMessage = "Token обязателен")]
    [MinLength(8, ErrorMessage = "Token слишком короткий")]
    [MaxLength(512, ErrorMessage = "Token максимум 512 символов")]
    string Token,

    [MaxLength(32, ErrorMessage = "Platform максимум 32 символа")]
    string Platform = "unknown"
);

/// <summary>Ключи Web Push subscription.</summary>
public record WebPushKeys(
    [Required(ErrorMessage = "p256dh обязателен")]
    [MaxLength(512)]
    string P256dh,

    [Required(ErrorMessage = "auth обязателен")]
    [MaxLength(512)]
    string Auth
);

/// <summary>Подписка Web Push (PWA). Формат как у PushManager.subscribe().</summary>
public record WebPushSubscriptionDto(
    [Required(ErrorMessage = "Endpoint обязателен")]
    [MaxLength(2048, ErrorMessage = "Endpoint слишком длинный")]
    [Url(ErrorMessage = "Endpoint должен быть URL")]
    string Endpoint,

    [Required(ErrorMessage = "Keys обязательны")]
    WebPushKeys Keys
);
