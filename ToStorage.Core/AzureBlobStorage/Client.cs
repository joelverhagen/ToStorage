using System;
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
        private const string LatestNumberMetadataKey = "LatestNumber";
        private readonly ISystemTime _systemTime;
        private readonly IPathBuilder _pathBuilder;

        public Client(ISystemTime systemTime, IPathBuilder pathBuilder)
        {
            _systemTime = systemTime;
            _pathBuilder = pathBuilder;
        }

        public async Task<UploadResult> UploadAsync(UploadRequest request)
        {
            // validate input
            _pathBuilder.Validate(request.PathFormat);

            // initialize
            request.Trace.Write("Initializing...");
            var context = new CloudContext(request.ConnectionString, request.Container);

            await context.BlobContainer.CreateIfNotExistsAsync(
                BlobContainerPublicAccessType.Blob,
                new BlobRequestOptions(),
                null);
            request.Trace.WriteLine(" done.");

            // set the direct
            var result = new UploadResult();
            CloudBlockBlob directBlob = null;
            if (request.UploadDirect)
            {
                if (request.Type == UploadRequestType.Number)
                {
                    directBlob = await UploadDirectNumberAsync(request, context, result);
                }
                else
                {
                    directBlob = await UploadDirectTimestampAsync(request, context);
                }
            }

            // set the latest
            CloudBlockBlob latestBlob = null;
            if (request.UploadLatest)
            {
                var latestPath = _pathBuilder.GetLatest(request.PathFormat);
                if (directBlob == null)
                {
                    latestBlob = await UploadBlobAsync(context, request, latestPath, direct: false);
                }
                else
                {
                    request.Trace.Write($"Copying the direct blob to the latest blob at '{latestPath}'...");
                    latestBlob = context.BlobContainer.GetBlockBlobReference(latestPath);
                    var sourceAccessCondition = new AccessCondition {IfMatchETag = directBlob.Properties.ETag};
                    var destAccessCondition = new AccessCondition {IfMatchETag = request.ETag};
                    await latestBlob.StartCopyAsync(directBlob, sourceAccessCondition, destAccessCondition, null, null);
                    while (latestBlob.CopyState.Status == CopyStatus.Pending)
                    {
                        await Task.Delay(100);
                        await latestBlob.ExistsAsync();
                    }
                    request.Trace.WriteLine(" done.");
                }
            }

            request.Trace.WriteLine();

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

        private async Task<CloudBlockBlob> UploadDirectTimestampAsync(UploadRequest request, CloudContext context)
        {
            var latestPath = _pathBuilder.GetDirect(request.PathFormat, _systemTime.UtcNow);

            var directBlob = await UploadBlobAsync(context, request, latestPath, direct: true);

            return directBlob;
        }

        private async Task<CloudBlockBlob> UploadDirectNumberAsync(UploadRequest request, CloudContext context, UploadResult result)
        {
            var latestNumberPath = _pathBuilder.GetDirect(request.PathFormat, 0);
            var latestNumberBlob = context.BlobContainer.GetBlockBlobReference(latestNumberPath);
            string latestNumberEtag = null;
            var latestNumber = 0;

            if (request.LatestNumber.HasValue)
            {
                // Use the provided version number
                latestNumberEtag = request.LatestNumberETag;
                latestNumber = request.LatestNumber.Value;
            }
            else
            {
                // Determine what version number what last used
                try
                {
                    await latestNumberBlob.FetchAttributesAsync();

                    latestNumberEtag = latestNumberBlob.Properties.ETag;

                    var latestNumberString = latestNumberBlob.Metadata[LatestNumberMetadataKey];
                    latestNumber = int.Parse(latestNumberString);
                }
                catch (StorageException e)
                {
                    if (e.RequestInformation.HttpStatusCode != 404)
                    {
                        throw;
                    }
                }

                latestNumber++;
            }

            // Upload the stream
            var latestPath = _pathBuilder.GetDirect(request.PathFormat, latestNumber);

            var directBlob = await UploadBlobAsync(context, request, latestPath, direct: true);
            
            // Update the latest version record
            AccessCondition accessCondition;
            if (!request.UseETags)
            {
                accessCondition = null;
            }
            else if (latestNumberEtag != null)
            {
                accessCondition = AccessCondition.GenerateIfMatchCondition(latestNumberEtag);
            }
            else
            {
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
            }

            latestNumberBlob.Metadata[LatestNumberMetadataKey] = latestNumber.ToString();

            await latestNumberBlob.UploadFromByteArrayAsync(new byte[0], 0, 0, accessCondition, options: null, operationContext: null);

            // Set the latest etag and number on the result
            result.LatestNumberETag = latestNumberBlob.Properties.ETag;
            result.LatestNumber = latestNumber;

            return directBlob;
        }

        private static async Task<CloudBlockBlob> UploadBlobAsync(CloudContext context, UploadRequest request, string blobPath, bool direct)
        {
            request.Trace.Write($"Uploading the blob at '{blobPath}'...");
            var blob = context.BlobContainer.GetBlockBlobReference(blobPath);

            AccessCondition accessCondition;
            if (!direct && !request.UseETags)
            {
                accessCondition = null;
            }
            if (direct || request.ETag == null)
            {
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
            }
            else
            {
                accessCondition = AccessCondition.GenerateIfMatchCondition(request.ETag);
            }

            // upload the blob
            await blob.UploadFromStreamAsync(request.Stream, accessCondition, options: null, operationContext: null);
            request.Trace.WriteLine(" done.");

            // set the content type
            if (!string.IsNullOrWhiteSpace(request.ContentType))
            {
                request.Trace.Write($"Setting the content type of '{blobPath}'...");
                blob.Properties.ContentType = request.ContentType;
                await blob.SetPropertiesAsync();
                request.Trace.WriteLine(" done.");
            }

            return blob;
        }

        public async Task<StreamResult> GetLatestStreamAsync(GetLatestRequest request)
        {
            var context = new CloudContext(request.ConnectionString, request.Container);

            var latestPath = _pathBuilder.GetLatest(request.PathFormat);
            var latestBlob = context.BlobContainer.GetBlockBlobReference(latestPath);

            try
            {
                var stream = await latestBlob.OpenReadAsync();
                return new StreamResult
                {
                    Stream = stream,
                    ETag = latestBlob.Properties.ETag,
                    ContentMD5 = latestBlob.Properties.ContentMD5
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
            
            var latestPath = _pathBuilder.GetLatest(request.PathFormat);
            var latestBlob = context.BlobContainer.GetBlockBlobReference(latestPath);

            return latestBlob.Uri;
        }

        public async Task<UriResult> GetLatestUriResultAsync(GetLatestRequest request)
        {
            var context = new CloudContext(request.ConnectionString, request.Container);

            var latestPath = _pathBuilder.GetLatest(request.PathFormat);
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
    }
}