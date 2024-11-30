namespace PoS_Placeholder.Server.Services;

using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;

public class ImageService
{
    private readonly string _connectionString;
    private readonly string _containerName;

    public ImageService(string connectionString, string containerName)
    {
        _connectionString = connectionString;
        _containerName = containerName;
    }

    public async Task UploadImageAsync(string filePath)
    {
        BlobContainerClient containerClient = new BlobContainerClient(_connectionString, _containerName);

        await containerClient.CreateIfNotExistsAsync();

        string fileName = Path.GetFileName(filePath);
        BlobClient blobClient = containerClient.GetBlobClient(fileName);

        using FileStream uploadFileStream = File.OpenRead(filePath);
        await blobClient.UploadAsync(uploadFileStream, overwrite: true);
    }

    //For testing
    public async Task ListImagesAsync()
    {
        BlobContainerClient containerClient = new BlobContainerClient(_connectionString, _containerName);
        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            Console.WriteLine($"Blob found: {blobItem.Name}");
        }
    }

    public async Task DownloadImageAsync(string blobName, string downloadFilePath)
    {
        BlobContainerClient containerClient = new BlobContainerClient(_connectionString, _containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DownloadToAsync(downloadFilePath);
    }
}