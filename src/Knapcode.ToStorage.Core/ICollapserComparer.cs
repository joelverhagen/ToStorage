using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core
{
    public interface ICollapserComparer : IComparer<string>
    {
        Task<bool> EqualsAsync(string nameX, Stream streamX, string nameY, Stream streamY, CancellationToken cancellationToken);
    }
}