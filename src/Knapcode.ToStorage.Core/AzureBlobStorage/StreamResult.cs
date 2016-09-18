using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class StreamResult : IDisposable
    {
        public Stream Stream { get; set; }
        public string ETag { get; set; }
        public string ContentMD5 { get; set; }

        public void Dispose()
        {
            Stream?.Dispose();
            Stream = null;
        }
    }
}
