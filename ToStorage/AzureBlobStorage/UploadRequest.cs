using System.IO;

namespace Knapcode.ToStorage.AzureBlobStorage
{
    public class UploadRequest
    {
        public string Container { get; set; }
        public string PathFormat { get; set; }
        public string ContentType { get; set; }
        public bool UpdateLatest { get; set; }
        public Stream Stream { get; set; }
        public TextWriter Trace { get; set; }
    }
}