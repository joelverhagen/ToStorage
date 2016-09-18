using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public interface IUniqueClient
    {
        Task<UploadResult> UploadAsync(UniqueUploadRequest request);
    }

    public class UniqueClient : IUniqueClient
    {
        private readonly IClient _innerClient;

        public UniqueClient(IClient innerClient)
        {
            _innerClient = innerClient;
        }

        public async Task<UploadResult> UploadAsync(UniqueUploadRequest request)
        {
            if (!request.Stream.CanRead)
            {
                throw new ArgumentException("The provided stream must be readable.");
            }

            using (request.Stream)
            {
                // get the current
                var getLatestRequest = new GetLatestRequest
                {
                    ConnectionString = request.ConnectionString,
                    Container = request.Container,
                    PathFormat = request.PathFormat,
                    Trace = request.Trace
                };

                request.Trace.Write("Gettings the existing latest...");
                using (var currentResult = await _innerClient.GetLatestStreamAsync(getLatestRequest))
                {
                    // return nothing if the streams are equivalent
                    if (currentResult != null)
                    {
                        if (!request.Stream.CanSeek)
                        {
                            throw new ArgumentException("The provided stream must be seekable");
                        }

                        // seek to the beginning for calculating the hash
                        request.Stream.Seek(0, SeekOrigin.Begin);
                        var contentMD5 = await GetStreamContentMD5(request.Stream);
                        if (contentMD5 == currentResult.ContentMD5)
                        {
                            request.Trace.WriteLine(" exactly the same! No upload required.");
                            return null;
                        }

                        // seek to the beginning for comparing the content
                        request.Stream.Seek(0, SeekOrigin.Begin);
                        if (await request.EqualsAsync(currentResult))
                        {
                            request.Trace.WriteLine(" equivalent! No upload required.");
                            return null;
                        }

                        request.Trace.WriteLine(" different! The provided content will be uploaded.");
                    }
                    else
                    {
                        request.Trace.WriteLine(" non-existent! The provided content will be uploaded.");
                    }

                    // Seek to the beginning for uploading the blob.
                    request.Stream.Seek(0, SeekOrigin.Begin);
                    var uploadRequest = new UploadRequest
                    {
                        ConnectionString = request.ConnectionString,
                        ETag = currentResult?.ETag,
                        UseETags = request.UseETag,
                        Stream = request.Stream,
                        PathFormat = request.PathFormat,
                        Container = request.Container,
                        Trace = request.Trace,
                        UploadDirect = request.UploadDirect,
                        UploadLatest = true,
                        ContentType = request.ContentType,
                        Type = request.Type
                    };

                    return await _innerClient.UploadAsync(uploadRequest);
                }
            }
        }

        private async Task<string> GetStreamContentMD5(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var buffer = new byte[8192];
                var read = buffer.Length;
                while (read > 0)
                {
                    read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    md5.TransformBlock(buffer, 0, read, null, 0);
                }

                md5.TransformFinalBlock(new byte[0], 0, 0);
                                
                var hashBytes = md5.Hash;
                var hashBase64 = Convert.ToBase64String(hashBytes);

                return hashBase64;
            } 
        }
    }
}
