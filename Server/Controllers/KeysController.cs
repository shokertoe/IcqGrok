using ICQ.Server.Data;
using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICQ.Server.Controllers;

/// <summary>
/// Публичные ключи E2E (ECDH): публикация своего bundle и получение ключа собеседника.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class KeysController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;

    public KeysController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPut("bundle")]
    [ProducesResponseType(typeof(KeyBundleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KeyBundleDto>> UploadBundle([FromBody] UploadKeyBundleRequest request)
    {
        var userId = _auth.GetUserIdFromPrincipal(User);
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.IdentityPublicKey) || request.IdentityPublicKey.Length > 512)
            return BadRequest(new { error = "Invalid public key" });

        var bundle = await _db.UserKeyBundles.FindAsync(userId.Value);
        if (bundle is null)
        {
            bundle = new UserKeyBundle
            {
                UserId = userId.Value,
                IdentityPublicKey = request.IdentityPublicKey.Trim(),
                UpdatedAt = DateTime.UtcNow
            };
            _db.UserKeyBundles.Add(bundle);
        }
        else
        {
            bundle.IdentityPublicKey = request.IdentityPublicKey.Trim();
            bundle.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new KeyBundleDto(bundle.UserId, bundle.IdentityPublicKey, bundle.UpdatedAt));
    }

    [HttpGet("bundle/{userId:guid}")]
    [ProducesResponseType(typeof(KeyBundleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KeyBundleDto>> GetBundle(Guid userId)
    {
        var bundle = await _db.UserKeyBundles.AsNoTracking().FirstOrDefaultAsync(k => k.UserId == userId);
        if (bundle is null)
            return NotFound(new { error = "User has no key bundle (E2E not enabled on their client)" });

        return Ok(new KeyBundleDto(bundle.UserId, bundle.IdentityPublicKey, bundle.UpdatedAt));
    }

    [HttpGet("me")]
    public async Task<ActionResult<object>> Me()
    {
        var userId = _auth.GetUserIdFromPrincipal(User);
        if (userId is null) return Unauthorized();

        var bundle = await _db.UserKeyBundles.AsNoTracking().FirstOrDefaultAsync(k => k.UserId == userId.Value);
        return Ok(new
        {
            hasKeys = bundle is not null,
            updatedAt = bundle?.UpdatedAt
        });
    }
}
