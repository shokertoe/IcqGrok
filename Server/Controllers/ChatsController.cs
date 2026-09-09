using ICQ.Server.Models;
using ICQ.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatsController : ApiControllerBase
{
    private readonly IChatService _chats;

    public ChatsController(IChatService chats, IAuthService auth) : base(auth)
    {
        _chats = chats;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ChatDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChatDto>>> GetMyChats()
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        return Ok(await _chats.GetUserChatsAsync(userId));
    }

    [HttpPost("private")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatDto>> CreatePrivate([FromBody] CreatePrivateChatRequest request)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        var (chat, error) = await _chats.GetOrCreatePrivateChatAsync(userId, request.TargetUserId);
        if (error is not null) return BadRequest(new { error });
        return Ok(chat);
    }

    [HttpPost("group")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatDto>> CreateGroup([FromBody] CreateGroupChatRequest request)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        var (chat, error) = await _chats.CreateGroupChatAsync(userId, request.Title, request.ParticipantIds);
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
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        limit = Math.Clamp(limit, 1, 200);
        return Ok(await _chats.GetMessagesAsync(userId, chatId, limit, before));
    }

    [HttpPost("messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        var (message, error) = await _chats.SendMessageAsync(userId, request);
        if (error is not null) return BadRequest(new { error });
        return Ok(message);
    }
}
