using ICQ.Server.Models;
using ICQ.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FilesController : ApiControllerBase
{
    private readonly IFileStorageService _storage;

    public FilesController(IFileStorageService storage, IAuthService auth) : base(auth)
    {
        _storage = storage;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(26_214_400)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponse>> Upload(IFormFile file)
    {
        if (!TryGetUserId(out var userId, out var unauthorized))
            return unauthorized!;

        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var result = await _storage.SaveAsync(file, userId);
        if (result is null)
            return BadRequest(new { error = "Invalid file type or size (max 25 MB)" });

        var (url, name, size) = result.Value;
        var isImage = IsImage(name);

        return Ok(new UploadResponse(url, name, size, isImage ? MessageType.Image : MessageType.File));
    }

    private static bool IsImage(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp";
    }
}

public record UploadResponse(
    string Url,
    string FileName,
    long Size,
    MessageType SuggestedType
);
