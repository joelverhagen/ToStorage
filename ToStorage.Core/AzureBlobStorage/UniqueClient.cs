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
                    if (currentResult != null && !await request.CompareAsync(currentResult.Stream))
                    {
                        return null;
                    }

                    if (currentResult != null)
                    {
                        request.ETag = currentResult.ETag;
                    }

                    return await _innerClient.UploadAsync(request);
                }
            }
        }
    }
}
