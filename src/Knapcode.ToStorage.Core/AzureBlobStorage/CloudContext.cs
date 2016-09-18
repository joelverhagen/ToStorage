using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class CloudContext
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