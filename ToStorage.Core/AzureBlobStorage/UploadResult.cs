using System;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public class UploadResult
    {
        public Uri DirectUrl { get; set; }
        public Uri LatestUrl { get; set; }
    }
}