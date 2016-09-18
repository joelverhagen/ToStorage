using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core
{
    public class OrdinalStreamEqualityComparer : IAsyncEqualityComparer<Stream>
    {
        public async Task<bool> EqualsAsync(Stream x, Stream y, CancellationToken cancellationToken)
        {
            var bufferX = new byte[8192];
            var bufferY = new byte[bufferX.Length];
            int readX = 1;
            int readY = 1;
            while (readX > 0 && readY > 0)
            {
                readX = await FillBufferAsync(x, bufferX);
                readY = await FillBufferAsync(y, bufferY);

                if (readX != readY || !bufferX.Take(readX).SequenceEqual(bufferY.Take(readY)))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<int> FillBufferAsync(Stream stream, byte[] buffer)
        {
            int offset = 0;
            int read = 1;
            while (offset < buffer.Length && read > 0)
            {
                read = await stream.ReadAsync(buffer, offset, buffer.Length - offset);
                offset += read;
            }

            return offset;
        }
    }
}
