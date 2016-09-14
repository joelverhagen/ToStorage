using System;
using System.IO;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class UniqueUploadRequest
    {
        public string ConnectionString { get; set; }
        public string Container { get; set; }
        public string PathFormat { get; set; }
        public TextWriter Trace { get; set; }
        public string ContentType { get; set; }
        public bool UseETag { get; set; } = true;
        public Stream Stream { get; set; }
        public bool UploadDirect { get; set; }
        public Func<StreamResult, Task<bool>> EqualsAsync { get; set; }
        public UploadRequestType Type { get; set; } = UploadRequestType.Timestamp;
    }
}
