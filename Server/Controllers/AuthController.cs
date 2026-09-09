using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var (response, error) = await _auth.RegisterAsync(request);
        if (error is not null)
            return BadRequest(new { error });
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var (response, error) = await _auth.LoginAsync(request);
        if (error is not null)
            return Unauthorized(new { error });
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var (response, error) = await _auth.RefreshAsync(request.RefreshToken);
        if (error is not null)
            return Unauthorized(new { error });
        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await _auth.RevokeRefreshTokenAsync(request.RefreshToken);
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = _auth.GetUserIdFromPrincipal(User);
        if (userId is null) return Unauthorized();
        return Ok(new { id = userId, nickname = User.Identity?.Name });
    }
}
