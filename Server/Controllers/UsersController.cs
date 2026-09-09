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

    private Guid UserId => _auth.GetUserIdFromPrincipal(User)
        ?? throw new UnauthorizedAccessException();

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchUsersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchUsersResponse>> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new SearchUsersResponse(new List<UserDto>()));

        var users = await _chats.SearchUsersAsync(q, UserId, limit);
        return Ok(new SearchUsersResponse(users));
    }

    [HttpGet("contacts")]
    [ProducesResponseType(typeof(List<ContactDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ContactDto>>> GetContacts()
    {
        var contacts = await _chats.GetContactsAsync(UserId);
        return Ok(contacts);
    }

    [HttpPost("contacts")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactDto>> AddContact([FromBody] AddContactRequest request)
    {
        var (contact, error) = await _chats.AddContactAsync(UserId, request.TargetUin);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    [HttpPost("contacts/{contactId:guid}/accept")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactDto>> AcceptContact(Guid contactId)
    {
        var (contact, error) = await _chats.AcceptContactAsync(UserId, contactId);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    [HttpPut("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        await _chats.UpdateUserStatusAsync(UserId, request.Status, request.StatusMessage);
        return Ok();
    }
}
