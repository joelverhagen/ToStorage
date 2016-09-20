using System;

namespace Knapcode.ToStorage.Core.Abstractions
{
    public interface IIncrementalHash : IDisposable
    {
        void AppendData(byte[] buffer, int start, int count);
        byte[] GetHashAndReset();
    }
}
