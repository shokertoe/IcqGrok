using ICQ.Server.Models;
using ICQ.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    public AuthController(IAuthService auth) : base(auth) { }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var (response, error) = await Auth.RegisterAsync(request);
        if (error is not null)
            return BadRequest(new { error });
        return Ok(response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var (response, error) = await Auth.LoginAsync(request);
        if (error is not null)
            return Unauthorized(new { error });
        return Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var (response, error) = await Auth.RefreshAsync(request.RefreshToken);
        if (error is not null)
            return Unauthorized(new { error });
        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await Auth.RevokeRefreshTokenAsync(request.RefreshToken);
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> Me()
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;

        return Ok(new
        {
            id = userId,
            nickname = User.Identity?.Name
        });
    }
}
