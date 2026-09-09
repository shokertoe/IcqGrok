using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

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
    public async Task<ActionResult<List<ChatDto>>> GetMyChats()
    {
        var list = await _chats.GetUserChatsAsync(UserId);
        return Ok(list);
    }

    [HttpPost("private")]
    public async Task<ActionResult<ChatDto>> CreatePrivate([FromBody] CreatePrivateChatRequest request)
    {
        var (chat, error) = await _chats.GetOrCreatePrivateChatAsync(UserId, request.TargetUserId);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

    [HttpPost("group")]
    public async Task<ActionResult<ChatDto>> CreateGroup([FromBody] CreateGroupChatRequest request)
    {
        var (chat, error) = await _chats.CreateGroupChatAsync(UserId, request.Title, request.ParticipantIds);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

    [HttpGet("{chatId:guid}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(
        Guid chatId,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null)
    {
        var messages = await _chats.GetMessagesAsync(UserId, chatId, limit, before);
        return Ok(messages);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request)
    {
        var (message, error) = await _chats.SendMessageAsync(UserId, request);
        if (error is not null) return BadRequest(new { error });
        return Ok(message);
    }
}
