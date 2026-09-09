using System.Security.Claims;
using ICQ.Server.Models;

namespace ICQ.Server.Services.Abstractions;

public interface IAuthService
{
    Task<(AuthResponse? Response, string? Error)> RegisterAsync(RegisterRequest request);
    Task<(AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request);
    Task<(AuthResponse? Response, string? Error)> RefreshAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Guid? GetUserIdFromPrincipal(ClaimsPrincipal principal);
}
