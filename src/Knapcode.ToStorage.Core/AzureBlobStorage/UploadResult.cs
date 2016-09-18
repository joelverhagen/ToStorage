using System;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class UploadResult
    {
        public Uri DirectUri { get; set; }
        public Uri LatestUri { get; set; }
        public string DirectETag { get; set; }
        public string LatestETag { get; set; }
        public string LatestNumberETag { get; set; }
        public int? LatestNumber { get; set; }
    }
}