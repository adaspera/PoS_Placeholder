using Azure.Storage.Blobs.Models;

namespace PoS_Placeholder.Server.Services;

using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;

public class ImageService : IImageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private string _containerName;

    public ImageService(string containerName,BlobServiceClient blobServiceClient)
    {
        _containerName = containerName;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> UploadFileBlobAsync(string blobName, IFormFile file)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();
        
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        var httpHeaders = new BlobHttpHeaders
        {
            ContentType = file.ContentType,
        };
        
        await blobClient.UploadAsync(file.OpenReadStream(), httpHeaders);
        
        return blobClient.Uri.AbsoluteUri;
    }

    public async Task<bool> DeleteFileBlobAsync(string blobName)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        return await blobClient.DeleteIfExistsAsync();
    }

    public async Task<string> GetFileBlobUrlAsync(string blobName)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.Uri.AbsoluteUri;
    }
}