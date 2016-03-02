using System.IO;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class Request
    {
        public string ConnectionString { get; set; }
        public string Container { get; set; }
        public string PathFormat { get; set; }
        public TextWriter Trace { get; set; }
    }
}