using System.IO;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class CollapseRequest
    {
        public string ConnectionString { get; set; }
        public string Container { get; set; }
        public string PathFormat { get; set; }
        public ICollapserComparer Comparer { get; set; }
        public TextWriter Trace { get; set; }
    }
}