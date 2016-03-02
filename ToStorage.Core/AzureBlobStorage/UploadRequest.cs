using System.IO;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class UploadRequest : Request
    {
        public string ContentType { get; set; }
        public bool UploadDirect { get; set; }
        public bool UploadLatest { get; set; }
        public string ETag { get; set; }
        public Stream Stream { get; set; }
    }
}