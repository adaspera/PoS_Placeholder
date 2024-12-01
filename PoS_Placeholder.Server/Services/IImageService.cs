namespace PoS_Placeholder.Server.Services;

public interface IImageService
{
    Task<string> UploadFileBlobAsync(string blobName, IFormFile file);
    Task<bool> DeleteFileBlobAsync(string blobName);
    Task<string> GetFileBlobUrlAsync(string blobName);
}