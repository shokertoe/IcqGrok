using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

/// <summary>
/// Регистрация, вход, обновление токенов и текущий пользователь.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>
    /// Регистрация нового пользователя (выдаётся UIN и пара access/refresh токенов).
    /// </summary>
    /// <param name="request">Никнейм и пароль.</param>
    /// <returns>Токены и профиль пользователя.</returns>
    /// <response code="200">Успешная регистрация.</response>
    /// <response code="400">Никнейм занят или невалидные данные.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var (response, error) = await _auth.RegisterAsync(request);
        if (error is not null)
            return BadRequest(new { error });
        return Ok(response);
    }

    /// <summary>
    /// Вход по никнейму (или UIN) и паролю.
    /// </summary>
    /// <param name="request">Логин и пароль.</param>
    /// <returns>Access и refresh токены.</returns>
    /// <response code="200">Успешный вход.</response>
    /// <response code="401">Неверные учётные данные.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var (response, error) = await _auth.LoginAsync(request);
        if (error is not null)
            return Unauthorized(new { error });
        return Ok(response);
    }

    /// <summary>
    /// Обновление access-токена по refresh-токену.
    /// </summary>
    /// <param name="request">Refresh-токен.</param>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var (response, error) = await _auth.RefreshAsync(request.RefreshToken);
        if (error is not null)
            return Unauthorized(new { error });
        return Ok(response);
    }

    /// <summary>
    /// Выход: отзыв refresh-токена.
    /// </summary>
    /// <param name="request">Refresh-токен для отзыва.</param>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await _auth.RevokeRefreshTokenAsync(request.RefreshToken);
        return Ok();
    }

    /// <summary>
    /// Текущий пользователь по JWT (id и nickname из claims).
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> Me()
    {
        var userId = _auth.GetUserIdFromPrincipal(User);
        if (userId is null) return Unauthorized();

        return Ok(new
        {
            id = userId,
            nickname = User.Identity?.Name
        });
    }
}
