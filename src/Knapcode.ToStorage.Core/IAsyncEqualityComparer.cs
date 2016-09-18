using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core
{
    public interface IAsyncEqualityComparer<in T>
    {
        Task<bool> EqualsAsync(T x, T y, CancellationToken cancellationToken);
    }
}