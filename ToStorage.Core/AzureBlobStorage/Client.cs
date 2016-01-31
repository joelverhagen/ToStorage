using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class Client
    {
        public async Task UploadAsync(string account, string key, UploadRequest request)
        {
            var storageCredentials = new StorageCredentials(account, key);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            await UploadAsync(cloudStorageAccount, request).ConfigureAwait(false);
        }

        public async Task UploadAsync(string connectionString, UploadRequest request)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            await UploadAsync(cloudStorageAccount, request).ConfigureAwait(false);
        }

        private async Task UploadAsync(CloudStorageAccount account, UploadRequest options)
        {
            // initialize
            var trace = options.Trace;
            options.Trace.Write("Initializing...");

            var cloudBlobClient = account.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(options.Container);

            await cloudBlobContainer.CreateIfNotExistsAsync(
                BlobContainerPublicAccessType.Blob,
                new BlobRequestOptions(),
                null).ConfigureAwait(false);
            trace.WriteLine(" done.");

            string directPath = string.Format(options.PathFormat, DateTimeOffset.UtcNow.ToString("yyyy.MM.dd.HH.mm.ss"));
            trace.Write($"Uploading the blob to {directPath}...");
            var directBlob = cloudBlobContainer.GetBlockBlobReference(directPath);

            // upload the blob
            using (var blobStream = await directBlob.OpenWriteAsync().ConfigureAwait(false))
            {
                await options.Stream.CopyToAsync(blobStream).ConfigureAwait(false);
            }

            trace.WriteLine(" done.");

            // set the content type
            if (!string.IsNullOrWhiteSpace(options.ContentType))
            {
                trace.Write("Setting the content type...");
                directBlob.Properties.ContentType = options.ContentType;
                await directBlob.SetPropertiesAsync().ConfigureAwait(false);
                trace.WriteLine(" done.");
            }

            // set the latest
            CloudBlockBlob latestBlob = null;
            if (options.UpdateLatest)
            {
                var latestPath = string.Format(options.PathFormat, "latest");
                trace.Write($"Updating {latestPath} to the latest blob...");
                latestBlob = cloudBlobContainer.GetBlockBlobReference(latestPath);
                await latestBlob.StartCopyAsync(directBlob).ConfigureAwait(false);
                while (latestBlob.CopyState.Status == CopyStatus.Pending)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
                trace.WriteLine(" done.");
            }

            trace.WriteLine();
            trace.WriteLine($"Direct: {directBlob.Uri}");
            if (latestBlob != null)
            {
                trace.WriteLine($"Latest: {latestBlob.Uri}");
            }
        }
    }
}