using System;
using System.IO;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core.Abstractions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public interface IClient
    {
        Task<UploadResult> UploadAsync(UploadRequest request);
        Task<StreamResult> GetLatestStreamAsync(GetLatestRequest request);
        Uri GetLatestUri(GetLatestRequest request);
        Task<UriResult> GetLatestUriResultAsync(GetLatestRequest request);
    }

    public class Client : IClient
    {
        private readonly ISystemTime _systemTime;

        public Client(ISystemTime systemTime)
        {
            _systemTime = systemTime;
        }

        public async Task<UploadResult> UploadAsync(UploadRequest request)
        {
            var context = new CloudContext(request.ConnectionString, request.Container);

            // initialize
            request.Trace.Write("Initializing...");

            await context.BlobContainer.CreateIfNotExistsAsync(
                BlobContainerPublicAccessType.Blob,
                new BlobRequestOptions(),
                null).ConfigureAwait(false);
            request.Trace.WriteLine(" done.");

            // set the direct
            CloudBlockBlob directBlob = null;
            if (request.UploadDirect)
            {
                var directPath = string.Format(request.PathFormat, _systemTime.UtcNow.ToString("yyyy.MM.dd.HH.mm.ss"));
                directBlob = await UploadBlobAsync(context, request, directPath);
            }

            // set the latest
            CloudBlockBlob latestBlob = null;
            if (request.UploadLatest)
            {
                var latestPath = GetLatestPath(request.PathFormat);
                if (directBlob == null)
                {
                    latestBlob = await UploadBlobAsync(context, request, latestPath);
                }
                else
                {
                    request.Trace.Write($"Copying the direct blob to the latest blob at '{latestPath}'...");
                    latestBlob = context.BlobContainer.GetBlockBlobReference(latestPath);
                    await latestBlob.StartCopyAsync(directBlob).ConfigureAwait(false);
                    while (latestBlob.CopyState.Status == CopyStatus.Pending)
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    request.Trace.WriteLine(" done.");
                }
            }

            request.Trace.WriteLine();

            var result = new UploadResult();

            if (directBlob != null)
            {
                result.DirectUri = directBlob.Uri;
                result.DirectETag = directBlob.Properties.ETag;
                request.Trace.WriteLine($"Direct: {directBlob.Uri}");
            }
            
            if (latestBlob != null)
            {
                result.LatestUri = latestBlob.Uri;
                result.LatestETag = latestBlob.Properties.ETag;
                request.Trace.WriteLine($"Latest: {latestBlob.Uri}");
            }

            return result;
        }

        private static async Task<CloudBlockBlob> UploadBlobAsync(CloudContext context, UploadRequest request, string blobPath)
        {
            request.Trace.Write($"Uploading the blob at '{blobPath}'...");
            var blob = context.BlobContainer.GetBlockBlobReference(blobPath);

            // upload the blob
            using (var blobStream = await blob.OpenWriteAsync().ConfigureAwait(false))
            {
                await request.Stream.CopyToAsync(blobStream).ConfigureAwait(false);
            }
            request.Trace.WriteLine(" done.");

            // set the content type
            if (!string.IsNullOrWhiteSpace(request.ContentType))
            {
                request.Trace.Write($"Setting the content type of '{blobPath}'...");
                blob.Properties.ContentType = request.ContentType;
                await blob.SetPropertiesAsync().ConfigureAwait(false);
                request.Trace.WriteLine(" done.");
            }

            return blob;
        }

        public async Task<StreamResult> GetLatestStreamAsync(GetLatestRequest request)
        {
            var context = new CloudContext(request.ConnectionString, request.Container);

            var latestPath = GetLatestPath(request.PathFormat);
            var latestBlob = context.BlobContainer.GetBlockBlobReference(latestPath);

            try
            {
                var stream = await latestBlob.OpenReadAsync();
                return new StreamResult
                {
                    Stream = stream,
                    ETag = latestBlob.Properties.ETag
                };
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    request.Trace.WriteLine($"The stream could not be found due to the following error: {e.RequestInformation.HttpStatusMessage}");
                    return null;
                }

                throw;
            }
        }

        public Uri GetLatestUri(GetLatestRequest request)
        {
            var context = new CloudContext(request.ConnectionString, request.Container);
            
            var latestPath = GetLatestPath(request.PathFormat);
            var latestBlob = context.BlobContainer.GetBlockBlobReference(latestPath);

            return latestBlob.Uri;
        }

        public async Task<UriResult> GetLatestUriResultAsync(GetLatestRequest request)
        {
            var context = new CloudContext(request.ConnectionString, request.Container);

            var latestPath = GetLatestPath(request.PathFormat);
            var latestBlob = context.BlobContainer.GetBlockBlobReference(latestPath);

            if (!await latestBlob.ExistsAsync())
            {
                return null;
            }

            return new UriResult
            {
                Uri = latestBlob.Uri,
                ETag = latestBlob.Properties.ETag
            };
        }

        private static string GetLatestPath(string pathFormat)
        {
            var latestPath = string.Format(pathFormat, "latest");
            return latestPath;
        }

        private class CloudContext
        {
            public CloudContext(string connectionString, string container)
            {
                StorageAccount = CloudStorageAccount.Parse(connectionString);
                BlobClient = StorageAccount.CreateCloudBlobClient();
                BlobContainer = BlobClient.GetContainerReference(container);
            }

            public CloudStorageAccount StorageAccount { get; }
            public CloudBlobClient BlobClient { get; }
            public CloudBlobContainer BlobContainer { get; }
        }
    }
}