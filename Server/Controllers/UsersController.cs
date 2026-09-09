using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

/// <summary>
/// Поиск пользователей, контакты и статус присутствия (ICQ-style).
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ChatService _chats;
    private readonly AuthService _auth;

    public UsersController(ChatService chats, AuthService auth)
    {
        _chats = chats;
        _auth = auth;
    }

    private Guid? CurrentUserId => _auth.GetUserIdFromPrincipal(User);

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SearchUsersResponse>> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new SearchUsersResponse(new List<UserDto>()));

        limit = Math.Clamp(limit, 1, 50);
        var users = await _chats.SearchUsersAsync(q, userId.Value, limit);
        return Ok(new SearchUsersResponse(users));
    }

    [HttpGet("contacts")]
    [ProducesResponseType(typeof(List<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ContactDto>>> GetContacts()
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        var contacts = await _chats.GetContactsAsync(userId.Value);
        return Ok(contacts);
    }

    [HttpPost("contacts")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ContactDto>> AddContact([FromBody] AddContactRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        var (contact, error) = await _chats.AddContactAsync(userId.Value, request.TargetUin);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    [HttpPost("contacts/{contactId:guid}/accept")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ContactDto>> AcceptContact(Guid contactId)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        var (contact, error) = await _chats.AcceptContactAsync(userId.Value, contactId);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    [HttpPut("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();
        if (!Enum.IsDefined(typeof(UserStatus), request.Status))
            return BadRequest(new { error = "Invalid status" });
        await _chats.UpdateUserStatusAsync(userId.Value, request.Status, request.StatusMessage);
        return Ok();
    }
}
