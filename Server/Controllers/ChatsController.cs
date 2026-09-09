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

    private Guid? CurrentUserId => _auth.GetUserIdFromPrincipal(User);

    [HttpGet]
    [ProducesResponseType(typeof(List<ChatDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ChatDto>>> GetMyChats()
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        var list = await _chats.GetUserChatsAsync(userId.Value);
        return Ok(list);
    }

    [HttpPost("private")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChatDto>> CreatePrivate([FromBody] CreatePrivateChatRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        var (chat, error) = await _chats.GetOrCreatePrivateChatAsync(userId.Value, request.TargetUserId);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

    [HttpPost("group")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChatDto>> CreateGroup([FromBody] CreateGroupChatRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        var (chat, error) = await _chats.CreateGroupChatAsync(userId.Value, request.Title, request.ParticipantIds);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

    [HttpGet("{chatId:guid}/messages")]
    [ProducesResponseType(typeof(List<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(
        Guid chatId,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        limit = Math.Clamp(limit, 1, 200);
        var messages = await _chats.GetMessagesAsync(userId.Value, chatId, limit, before);
        return Ok(messages);
    }

    [HttpPost("messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        var (message, error) = await _chats.SendMessageAsync(userId.Value, request);
        if (error is not null) return BadRequest(new { error });
        return Ok(message);
    }
}
