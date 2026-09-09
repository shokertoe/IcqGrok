using ICQ.Server.Models;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

/// <summary>
/// Загрузка файлов и изображений (multipart). URL можно передать в SendMessage.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly FileStorageService _storage;
    private readonly AuthService _auth;

    public FilesController(FileStorageService storage, AuthService auth)
    {
        _storage = storage;
        _auth = auth;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(26_214_400)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponse>> Upload(IFormFile file)
    {
        var userId = _auth.GetUserIdFromPrincipal(User);
        if (userId is null) return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var result = await _storage.SaveAsync(file, userId.Value);
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
