using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core
{
    public class AdapterCollapserComparer : ICollapserComparer
    {
        private readonly IComparer<string> _nameComparer;
        private readonly IAsyncEqualityComparer<Stream> _streamComparer;

        public AdapterCollapserComparer(IComparer<string> nameComparer, IAsyncEqualityComparer<Stream> streamComparer)
        {
            _nameComparer = nameComparer;
            _streamComparer = streamComparer;
        }

        public int Compare(string nameX, string nameY)
        {
            return _nameComparer.Compare(nameX, nameY);
        }

        public async Task<bool> EqualsAsync(string nameX, Stream streamX, string nameY, Stream streamY, CancellationToken cancellationToken)
        {
            return await _streamComparer.EqualsAsync(streamX, streamY, cancellationToken);
        }
    }
}