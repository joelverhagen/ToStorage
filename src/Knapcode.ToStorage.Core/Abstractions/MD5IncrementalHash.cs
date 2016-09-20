using System;
using System.Security.Cryptography;

namespace Knapcode.ToStorage.Core.Abstractions
{
    public class MD5IncrementalHash : IIncrementalHash
    {
#if NET_FRAMEWORK
        private readonly MD5 _implementation;

        public MD5IncrementalHash()
        {
            _implementation = MD5.Create();
        }

        public void AppendData(byte[] data, int offset, int count)
        {
            _implementation.TransformBlock(data, offset, count, null, 0);
        }

        public byte[] GetHashAndReset()
        {
            _implementation.TransformFinalBlock(new byte[0], 0, 0);
            var hash = _implementation.Hash;
            _implementation.Initialize();

            return hash;
        }

        public void Dispose()
        {
            _implementation.Dispose();
        }
#elif NET_CORE
        private readonly IncrementalHash _implementation;

        public MD5IncrementalHash()
        {
            _implementation = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        }

        public void AppendData(byte[] data, int offset, int count)
        {
            _implementation.AppendData(data, offset, count);
        }

        public byte[] GetHashAndReset()
        {
            return _implementation.GetHashAndReset();
        }

        public void Dispose()
        {
            _implementation.Dispose();
        }
#endif
    }
}
