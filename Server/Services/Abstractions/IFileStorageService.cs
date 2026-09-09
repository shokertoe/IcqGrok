namespace ICQ.Server.Services.Abstractions;

public interface IFileStorageService
{
    Task<(string Url, string FileName, long Size)?> SaveAsync(IFormFile file, Guid userId);
    bool Delete(string relativeOrFileName);
}
