using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ICQ.Server.Data;
using ICQ.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ICQ.Server.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<(AuthResponse? Response, string? Error)> RegisterAsync(RegisterRequest request)
    {
        var nickname = request.Nickname.Trim();
        if (await _db.Users.AnyAsync(u => u.Nickname.ToLower() == nickname.ToLower()))
            return (null, "Nickname already taken");

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            await _db.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == request.Email.ToLower()))
            return (null, "Email already registered");

        var maxUin = await _db.Users.MaxAsync(u => (long?)u.Uin) ?? 99999;
        var uin = maxUin + 1;

        var user = new User
        {
            Uin = uin,
            Nickname = nickname,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email?.Trim(),
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            Status = UserStatus.Offline
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Nickname} (UIN {Uin})", user.Nickname, user.Uin);

        return (await GenerateAuthResponseAsync(user), null);
    }

    public async Task<(AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request)
    {
        var login = request.NicknameOrEmail.Trim().ToLower();

        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Nickname.ToLower() == login ||
                (u.Email != null && u.Email.ToLower() == login));

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (null, "Invalid credentials");

        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (await GenerateAuthResponseAsync(user), null);
    }

    public async Task<(AuthResponse? Response, string? Error)> RefreshAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);

        if (token is null || token.ExpiresAt < DateTime.UtcNow)
            return (null, "Invalid or expired refresh token");

        token.IsRevoked = true;
        await _db.SaveChangesAsync();

        return (await GenerateAuthResponseAsync(token.User), null);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token is not null)
        {
            token.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var accessToken = GenerateJwt(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(
            double.Parse(_config["Jwt:AccessTokenMinutes"] ?? "60", CultureInfo.InvariantCulture));

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(
                double.Parse(_config["Jwt:RefreshTokenDays"] ?? "30", CultureInfo.InvariantCulture))
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return new AuthResponse(
            accessToken,
            refreshToken.Token,
            expiresAt,
            MapToDto(user)
        );
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is not configured")));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Nickname),
            new Claim("uin", user.Uin.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "ICQ.Server",
            audience: _config["Jwt:Audience"] ?? "ICQ.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:AccessTokenMinutes"] ?? "60", CultureInfo.InvariantCulture)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static UserDto MapToDto(User user) => new(
        user.Id,
        user.Uin,
        user.Nickname,
        user.Email,
        user.FirstName,
        user.LastName,
        user.StatusMessage,
        user.Status,
        user.LastSeenAt,
        user.AvatarUrl
    );

    public Guid? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
