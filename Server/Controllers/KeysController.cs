using ICQ.Server.Data;
using ICQ.Server.Models;
using ICQ.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICQ.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class KeysController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public KeysController(AppDbContext db, IAuthService auth) : base(auth)
    {
        _db = db;
    }

    [HttpPut("bundle")]
    [ProducesResponseType(typeof(KeyBundleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KeyBundleDto>> UploadBundle([FromBody] UploadKeyBundleRequest request)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;

        if (string.IsNullOrWhiteSpace(request.IdentityPublicKey) || request.IdentityPublicKey.Length > 512)
            return BadRequest(new { error = "Invalid public key" });

        var bundle = await _db.UserKeyBundles.FindAsync(userId);
        if (bundle is null)
        {
            bundle = new UserKeyBundle
            {
                UserId = userId,
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
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;

        var bundle = await _db.UserKeyBundles.AsNoTracking().FirstOrDefaultAsync(k => k.UserId == userId);
        return Ok(new
        {
            hasKeys = bundle is not null,
            updatedAt = bundle?.UpdatedAt
        });
    }
}
