using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

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
    public async Task<ActionResult<SearchUsersResponse>> Search([FromQuery] string q, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new SearchUsersResponse(new List<UserDto>()));
        var users = await _chats.SearchUsersAsync(q, UserId, limit);
        return Ok(new SearchUsersResponse(users));
    }

    [HttpGet("contacts")]
    public async Task<ActionResult<List<ContactDto>>> GetContacts()
    {
        return Ok(await _chats.GetContactsAsync(UserId));
    }

    [HttpPost("contacts")]
    public async Task<ActionResult<ContactDto>> AddContact([FromBody] AddContactRequest request)
    {
        var (contact, error) = await _chats.AddContactAsync(UserId, request.TargetUin);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    [HttpPost("contacts/{contactId:guid}/accept")]
    public async Task<ActionResult<ContactDto>> AcceptContact(Guid contactId)
    {
        var (contact, error) = await _chats.AcceptContactAsync(UserId, contactId);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        await _chats.UpdateUserStatusAsync(UserId, request.Status, request.StatusMessage);
        return Ok();
    }
}
