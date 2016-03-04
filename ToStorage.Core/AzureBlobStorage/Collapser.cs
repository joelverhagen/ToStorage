using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class Collapser
    {
        private readonly IPathBuilder _pathBuilder;

        public Collapser(IPathBuilder pathBuilder)
        {
            _pathBuilder = pathBuilder;
        }

        public async Task CollapseAsync(CollapseRequest request)
        {
            _pathBuilder.Validate(request.PathFormat);

            var context = new CloudContext(request.ConnectionString, request.Container);

            if (!await context.BlobContainer.ExistsAsync())
            {
                request.Trace.WriteLine($"The container {request.Container} does not exist so no collapsing is necessary.");
                return;
            }

            // determine the prefix
            var placeholderIndex = request.PathFormat.IndexOf("{0}", StringComparison.Ordinal);
            var prefix = request.PathFormat.Substring(0, placeholderIndex);
            var suffix = request.PathFormat.Substring(placeholderIndex + "{0}".Length);
            var latestPath = _pathBuilder.GetLatest(request.PathFormat);

            // collect and sort all of the blob names
            var blobNames = new List<string>();
            var token = (BlobContinuationToken) null;
            do
            {
                var segment = await context.BlobContainer.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.All, null, token, null, null);
                token = segment.ContinuationToken;

                // filter out packages that don't match the path format and the latest
                var segmentBlobNames = segment
                    .Results
                    .OfType<ICloudBlob>()
                    .Select(x => x.Name)
                    .Where(x => x.EndsWith(suffix) && x != latestPath);

                int before = blobNames.Count;
                blobNames.AddRange(segmentBlobNames);
                int added = blobNames.Count - before;
                request.Trace.WriteLine($"Fetched {added} blobs.");
            }
            while (token != null);

            blobNames.Sort(request.Comparer);

            // collapse the blobs
            int indexX = 0;
            int indexY = 1;
            while(indexX < blobNames.Count - 1 && indexY < blobNames.Count)
            {
                var nameX = blobNames[indexX];
                var nameY = blobNames[indexY];

                var blobX = context.BlobContainer.GetBlockBlobReference(nameX);
                var blobY = context.BlobContainer.GetBlockBlobReference(nameY);

                using (var streamX = await blobX.OpenReadAsync())
                using (var streamY = await blobY.OpenReadAsync())
                {
                    var equals = await request.Comparer.EqualsAsync(nameX, streamX, nameY, streamY, CancellationToken.None);
                    if (equals)
                    {
                        await blobY.DeleteAsync();
                        indexY++;
                    }
                    else
                    {
                        indexX = indexY;
                        indexY++;
                    }
                }
            }
        }
    }
}
