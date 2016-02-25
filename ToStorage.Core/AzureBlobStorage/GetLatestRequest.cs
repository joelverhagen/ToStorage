using System.IO;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class GetLatestRequest
    {
        public string Container { get; set; }
        public string PathFormat { get; set; }
        public TextWriter Trace { get; set; }
    }
}