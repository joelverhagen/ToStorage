using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core
{
    public class OrdinalCollapserComparer : ICollapserComparer
    {
        private readonly StringComparer _nameComparer;
        private readonly OrdinalStreamEqualityComparer _streamComparer;

        public OrdinalCollapserComparer()
        {
            _nameComparer = StringComparer.Ordinal;
            _streamComparer = new OrdinalStreamEqualityComparer();
        }

        public int Compare(string x, string y)
        {
            return _nameComparer.Compare(x, y);
        }

        public async Task<bool> EqualsAsync(string nameX, Stream streamX, string nameY, Stream streamY, CancellationToken cancellationToken)
        {
            return await _streamComparer.EqualsAsync(streamX, streamY, cancellationToken);
        }
    }
}