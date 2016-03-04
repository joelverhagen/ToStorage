using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core
{
    public class CollapserComparer : ICollapserComparer
    {
        private readonly AsyncStreamEqualityComparer _comparer;

        public CollapserComparer()
        {
            _comparer = new AsyncStreamEqualityComparer();
        }

        public int Compare(string nameX, string nameY)
        {
            return StringComparer.Ordinal.Compare(nameX, nameY);
        }

        public async Task<bool> EqualsAsync(string nameX, Stream streamX, string nameY, Stream streamY, CancellationToken cancellationToken)
        {
            return await _comparer.EqualsAsync(streamX, streamY, cancellationToken);
        }
    }
}