using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core
{
    public interface IAsyncEqualityComparer
    {
        Task<bool> EqualsAsync(Stream x, Stream y, CancellationToken cancellationToken);
        Task<int> GetHashCodeAsync(Stream obj, CancellationToken cancellationToken);
    }
}