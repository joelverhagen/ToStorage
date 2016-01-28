using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Knapcode.ToStorage.AzureBlobStorage
{
    public class Client
    {
        public async Task UploadAsync(Options options, Stream stream)
        {
            // initialize
            CloudStorageAccount cloudStorageAccount;
            if (options.ConnectionString != null)
            {
                cloudStorageAccount = CloudStorageAccount.Parse(options.ConnectionString);
            }
            else if (options.Account != null && options.Key != null)
            {
                var storageCredentials = new StorageCredentials(options.Account, options.Key);
                cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            }
            else
            {
                throw new ArgumentException("A connection string or account and key must be specified.", nameof(options));
            }

            
            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(options.Container);

            await cloudBlobContainer.CreateIfNotExistsAsync(
                BlobContainerPublicAccessType.Blob,
                new BlobRequestOptions(),
                null);

            string directPath = string.Format(options.PathFormat, DateTimeOffset.UtcNow.ToString("yyyy.MM.dd.HH.mm.ss"));
            var directBlob = cloudBlobContainer.GetBlockBlobReference(directPath);

            // upload the blob
            using (var blobStream = await directBlob.OpenWriteAsync())
            {
                await stream.CopyToAsync(blobStream);
            }

            // set the content type
            if (!string.IsNullOrWhiteSpace(options.ContentType))
            {
                directBlob.Properties.ContentType = options.ContentType;
                await directBlob.SetPropertiesAsync();
            }

            // set the latest
            if (options.UpdateLatest)
            {
                var latestBlob = cloudBlobContainer.GetBlockBlobReference(string.Format(options.PathFormat, "latest"));
                await latestBlob.StartCopyAsync(directBlob);
                while (latestBlob.CopyState.Status == CopyStatus.Pending)
                {
                    await Task.Delay(100);
                }
            }
        }
    }
}