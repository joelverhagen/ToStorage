using System.Linq;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Knapcode.ToStorage.Core.Tests
{
    public static class TestSupport
    {
        public const string ConnectionString = "UseDevelopmentStorage=true";
        public const string Container = "testcontainer";

        public static void DeleteBlobsWithPrefix(string pathPrefix)
        {
            var context = new CloudContext(ConnectionString, Container);
            var blobs = context.BlobContainer.ListBlobs(pathPrefix, useFlatBlobListing: true);
            foreach (var blob in blobs.OfType<CloudBlockBlob>())
            {
                blob.Delete();
            }
        }
    }
}
