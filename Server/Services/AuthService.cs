using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ICQ.Server.Data;
using ICQ.Server.Models;

namespace ICQ.Server.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return null;

        var uin = await GenerateUniqueUinAsync();
        var user = new User
        {
            Uin = uin,
            Email = req.Email.Trim().ToLowerInvariant(),
            PasswordHash = HashPassword(req.Password),
            Nickname = req.Nickname.Trim(),
            Status = "online",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest req)
    {
        User? user = null;
        if (int.TryParse(req.EmailOrUin, out var uin))
            user = await _db.Users.FirstOrDefaultAsync(u => u.Uin == uin);
        if (user == null)
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.EmailOrUin.Trim().ToLowerInvariant());

        if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
            return null;

        user.IsOnline = true;
        user.Status = "online";
        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        var rt = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);
        if (rt == null) return null;

        rt.IsRevoked = true;
        await _db.SaveChangesAsync();
        return await IssueTokensAsync(rt.User);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var access = GenerateJwt(user);
        var refresh = GenerateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refresh,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresIn = 3600 * 24,
            User = new UserDto
            {
                Id = user.Id,
                Uin = user.Uin,
                Email = user.Email,
                Nickname = user.Nickname,
                Status = user.Status,
                StatusMessage = user.StatusMessage,
                IsOnline = user.IsOnline,
                LastSeenAt = user.LastSeenAt
            }
        };
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("sub", user.Id.ToString()),
            new Claim("uin", user.Uin.ToString()),
            new Claim(ClaimTypes.Name, user.Nickname),
            new Claim(ClaimTypes.Email, user.Email)
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private async Task<int> GenerateUniqueUinAsync()
    {
        var rnd = new Random();
        int uin;
        do
        {
            uin = rnd.Next(100000, 99999999);
        } while (await _db.Users.AnyAsync(u => u.Uin == uin));
        return uin;
    }

    private static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var test = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, test);
    }
}
