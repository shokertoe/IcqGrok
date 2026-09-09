using ICQ.Server.Models;
using ICQ.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ApiControllerBase
{
    private readonly IChatService _chats;

    public UsersController(IChatService chats, IAuthService auth) : base(auth)
    {
        _chats = chats;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchUsersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchUsersResponse>> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new SearchUsersResponse(new List<UserDto>()));

        limit = Math.Clamp(limit, 1, 50);
        return Ok(new SearchUsersResponse(await _chats.SearchUsersAsync(q, userId, limit)));
    }

    [HttpGet("contacts")]
    [ProducesResponseType(typeof(List<ContactDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ContactDto>>> GetContacts()
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        return Ok(await _chats.GetContactsAsync(userId));
    }

    [HttpPost("contacts")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactDto>> AddContact([FromBody] AddContactRequest request)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        var (contact, error) = await _chats.AddContactAsync(userId, request.TargetUin);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    [HttpPost("contacts/{contactId:guid}/accept")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactDto>> AcceptContact(Guid contactId)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        var (contact, error) = await _chats.AcceptContactAsync(userId, contactId);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    [HttpPut("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;
        if (!Enum.IsDefined(typeof(UserStatus), request.Status))
            return BadRequest(new { error = "Invalid status" });
        await _chats.UpdateUserStatusAsync(userId, request.Status, request.StatusMessage);
        return Ok();
    }
}
