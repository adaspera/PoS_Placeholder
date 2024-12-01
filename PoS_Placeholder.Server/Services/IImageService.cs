namespace PoS_Placeholder.Server.Services;

public interface IImageService
{
    Task<string> UploadFileBlobAsync(string blobName, string containerName, IFormFile file);
    Task<bool> DeleteFileBlobAsync(string blobName, string containerName);
    Task<string> GetFileBlobUrlAsync(string blobName, string containerName);
}