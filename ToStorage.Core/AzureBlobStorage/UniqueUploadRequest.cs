using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class UniqueUploadRequest : UploadRequest
    {
        public Func<Stream, Task<bool>> CompareAsync { get; set; }
    }
}
