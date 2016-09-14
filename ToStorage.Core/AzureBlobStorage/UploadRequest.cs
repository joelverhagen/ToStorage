using System.IO;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class UploadRequest
    {
        public string ConnectionString { get; set; }
        public string Container { get; set; }
        public string PathFormat { get; set; }
        public TextWriter Trace { get; set; }
        public string ContentType { get; set; }
        public bool UploadDirect { get; set; }
        public bool UploadLatest { get; set; }
        public string ETag { get; set; }
        public bool UseETag { get; set; } = true;
        public Stream Stream { get; set; }
        public UploadRequestType Type { get; set; } = UploadRequestType.Timestamp;
    }
}