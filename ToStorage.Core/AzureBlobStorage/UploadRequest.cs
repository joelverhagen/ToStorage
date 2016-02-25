using System.IO;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class UploadRequest
    {
        public string Container { get; set; }
        public string PathFormat { get; set; }
        public string ContentType { get; set; }
        public bool UploadDirect { get; set; }
        public bool UploadLatest { get; set; }
        public Stream Stream { get; set; }
        public TextWriter Trace { get; set; }
    }
}