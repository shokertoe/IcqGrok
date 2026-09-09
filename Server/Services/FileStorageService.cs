using ICQ.Server.Services.Abstractions;

namespace ICQ.Server.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _storagePath;
    private readonly string _baseUrl;
    private readonly ILogger<FileStorageService> _logger;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        ".pdf", ".txt", ".zip", ".rar", ".7z",
        ".doc", ".docx", ".xls", ".xlsx",
        ".mp3", ".ogg", ".wav", ".mp4", ".webm"
    };
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    public FileStorageService(IConfiguration config, ILogger<FileStorageService> logger)
    {
        _storagePath = config["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        _baseUrl = config["FileStorage:BaseUrl"] ?? "/uploads";
        _logger = logger;

        Directory.CreateDirectory(_storagePath);
    }

    public async Task<(string Url, string FileName, long Size)?> SaveAsync(IFormFile file, Guid userId)
    {
        if (file.Length == 0 || file.Length > MaxFileSizeBytes)
            return null;

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return null;

        var safeName = $"{userId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_storagePath, safeName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        _logger.LogInformation("Saved file {File} ({Size} bytes) for user {UserId}", safeName, file.Length, userId);

        var url = $"{_baseUrl.TrimEnd('/')}/{safeName}";
        return (url, file.FileName, file.Length);
    }

    public bool Delete(string relativeOrFileName)
    {
        var name = Path.GetFileName(relativeOrFileName);
        var fullPath = Path.Combine(_storagePath, name);
        if (!File.Exists(fullPath)) return false;
        File.Delete(fullPath);
        return true;
    }
}
