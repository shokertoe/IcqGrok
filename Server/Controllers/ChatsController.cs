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

    [HttpGet]
    [ProducesResponseType(typeof(List<ChatDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChatDto>>> GetMyChats()
    {
        var list = await _chats.GetUserChatsAsync(UserId);
        return Ok(list);
    }

    [HttpPost("private")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatDto>> CreatePrivate([FromBody] CreatePrivateChatRequest request)
    {
        var (chat, error) = await _chats.GetOrCreatePrivateChatAsync(UserId, request.TargetUserId);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

    [HttpPost("group")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatDto>> CreateGroup([FromBody] CreateGroupChatRequest request)
    {
        var (chat, error) = await _chats.CreateGroupChatAsync(UserId, request.Title, request.ParticipantIds);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

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
