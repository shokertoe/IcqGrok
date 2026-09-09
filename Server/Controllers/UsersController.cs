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

    /// <summary>Поиск пользователей по никнейму или UIN.</summary>
    /// <param name="q">Строка поиска.</param>
    /// <param name="limit">Лимит результатов (по умолчанию 20).</param>
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

    /// <summary>Список контактов текущего пользователя.</summary>
    [HttpGet("contacts")]
    [ProducesResponseType(typeof(List<ContactDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ContactDto>>> GetContacts()
    {
        var contacts = await _chats.GetContactsAsync(UserId);
        return Ok(contacts);
    }

    /// <summary>Добавить контакт по UIN.</summary>
    [HttpPost("contacts")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactDto>> AddContact([FromBody] AddContactRequest request)
    {
        var (contact, error) = await _chats.AddContactAsync(UserId, request.TargetUin);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    /// <summary>Принять входящий запрос в контакты.</summary>
    [HttpPost("contacts/{contactId:guid}/accept")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactDto>> AcceptContact(Guid contactId)
    {
        var (contact, error) = await _chats.AcceptContactAsync(UserId, contactId);
        if (error is not null) return BadRequest(new { error });
        return Ok(contact);
    }

    /// <summary>Обновить статус и статусное сообщение (Online / Away / DND и т.д.).</summary>
    [HttpPut("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        await _chats.UpdateUserStatusAsync(UserId, request.Status, request.StatusMessage);
        return Ok();
    }
}
