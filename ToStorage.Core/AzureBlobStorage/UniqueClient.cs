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

                using (var currentResult = await _innerClient.GetLatestStreamAsync(getLatestRequest))
                {
                    // return nothing if the streams are equivalent
                    if (currentResult != null && await request.EqualsAsync(currentResult))
                    {
                        return null;
                    }

                    var uploadRequest = new UploadRequest
                    {
                        ConnectionString = request.ConnectionString,
                        ETag = currentResult?.ETag,
                        Stream = request.Stream,
                        PathFormat = request.PathFormat,
                        Container = request.Container,
                        Trace = request.Trace,
                        UploadDirect = request.UploadDirect,
                        UploadLatest = true,
                        ContentType = request.ContentType
                    };

                    return await _innerClient.UploadAsync(uploadRequest);
                }
            }
        }
    }
}
