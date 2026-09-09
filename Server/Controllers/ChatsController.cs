using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

/// <summary>
/// Чаты и сообщения: список диалогов, личные/групповые чаты, история и отправка.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly ChatService _chats;
    private readonly AuthService _auth;

    public ChatsController(ChatService chats, AuthService auth)
    {
        _chats = chats;
        _auth = auth;
    }

    private Guid UserId => _auth.GetUserIdFromPrincipal(User)
        ?? throw new UnauthorizedAccessException();

    /// <summary>
    /// Список чатов текущего пользователя.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ChatDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChatDto>>> GetMyChats()
    {
        var list = await _chats.GetUserChatsAsync(UserId);
        return Ok(list);
    }

    /// <summary>
    /// Создать или получить существующий личный чат с пользователем.
    /// </summary>
    /// <param name="request">Id собеседника.</param>
    [HttpPost("private")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatDto>> CreatePrivate([FromBody] CreatePrivateChatRequest request)
    {
        var (chat, error) = await _chats.GetOrCreatePrivateChatAsync(UserId, request.TargetUserId);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

    /// <summary>
    /// Создать групповой чат.
    /// </summary>
    /// <param name="request">Название и участники.</param>
    [HttpPost("group")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatDto>> CreateGroup([FromBody] CreateGroupChatRequest request)
    {
        var (chat, error) = await _chats.CreateGroupChatAsync(UserId, request.Title, request.ParticipantIds);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

    /// <summary>
    /// История сообщений чата (пагинация по <paramref name="before"/>).
    /// </summary>
    /// <param name="chatId">Id чата.</param>
    /// <param name="limit">Максимум сообщений (по умолчанию 50).</param>
    /// <param name="before">Вернуть сообщения старше этой даты (UTC).</param>
    [HttpGet("{chatId:guid}/messages")]
    [ProducesResponseType(typeof(List<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(
        Guid chatId,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null)
    {
        var messages = await _chats.GetMessagesAsync(UserId, chatId, limit, before);
        return Ok(messages);
    }

    /// <summary>
    /// Отправить сообщение (текст, файл, E2E-ciphertext).
    /// </summary>
    /// <param name="request">Содержимое сообщения и id чата.</param>
    [HttpPost("messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request)
    {
        var (message, error) = await _chats.SendMessageAsync(UserId, request);
        if (error is not null) return BadRequest(new { error });
        return Ok(message);
    }
}
