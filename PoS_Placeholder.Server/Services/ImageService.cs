using Azure.Storage.Blobs.Models;

namespace PoS_Placeholder.Server.Services;

using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;

public class ImageService : IImageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public ImageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> UploadFileBlobAsync(string blobName, string containerName, IFormFile file)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();
        
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        var httpHeaders = new BlobHttpHeaders
        {
            ContentType = file.ContentType,
        };
        
        await blobClient.UploadAsync(file.OpenReadStream(), httpHeaders);
        
        return blobClient.Uri.AbsoluteUri;
    }

    public async Task<bool> DeleteFileBlobAsync(string blobName, string containerName)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        return await blobClient.DeleteIfExistsAsync();
    }

    public async Task<string> GetFileBlobUrlAsync(string blobName, string containerName)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.Uri.AbsoluteUri;
    }
}