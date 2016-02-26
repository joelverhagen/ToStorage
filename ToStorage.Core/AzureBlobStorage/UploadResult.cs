using System;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class UploadResult
    {
        public Uri DirectUri { get; set; }
        public Uri LatestUri { get; set; }
    }
}