using System.Text;
using Knapcode.ToStorage.Core.Abstractions;
using Xunit;

namespace Knapcode.ToStorage.Core.Test.Abstractions
{
    public class MD5IncrementalHashTests
    {
        private static readonly byte[] Foobar = Encoding.ASCII.GetBytes("foobar");
        private static readonly byte[] HashOfFoobar = new byte[]
        {
            0x38, 0x58, 0xf6, 0x22, 0x30, 0xac, 0x3c, 0x91,
            0x5f, 0x30, 0x0c, 0x66, 0x43, 0x12, 0xc6, 0x3f
        };

        private static readonly byte[] Empty = new byte[0];
        private static readonly byte[] HashOfEmpty = new byte[]
        {
            0xd4, 0x1d, 0x8c, 0xd9, 0x8f, 0x00, 0xb2, 0x04,
            0xe9, 0x80, 0x09, 0x98, 0xec, 0xf8, 0x42, 0x7e
        };

        [Fact]
        public void ProducesKnownHash()
        {
            // Arrange
            using (var target = new MD5IncrementalHash())
            {
                // Act
                target.AppendData(Foobar, 0, Foobar.Length);
                var actual = target.GetHashAndReset();

                // Assert
                Assert.Equal(HashOfFoobar, actual);
            }
        }

        [Fact]
        public void HashesPartOfTheProvidedBuffer()
        {
            // Arrange
            using (var target = new MD5IncrementalHash())
            {
                var expected = new byte[] // MD5 hash of "ooba"
                {
                    0xdb, 0xec, 0xe8, 0x01, 0xac, 0xcd, 0x4d, 0xb2,
                    0xed, 0x8c, 0x1c, 0x86, 0x3f, 0x5a, 0xad, 0x92
                };

                // Act
                target.AppendData(Foobar, 1, 4);
                var actual = target.GetHashAndReset();

                // Assert
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void HashesEmptyBuffer()
        {
            // Arrange
            using (var target = new MD5IncrementalHash())
            {
                // Act
                target.AppendData(Empty, 0, 0);
                var actual = target.GetHashAndReset();

                // Assert
                Assert.Equal(HashOfEmpty, actual);
            }
        }

        [Fact]
        public void ResetAllowsReuse()
        {
            // Arrange
            using (var target = new MD5IncrementalHash())
            {
                var firstInput = Encoding.ASCII.GetBytes("ignored");                

                // Act
                target.AppendData(firstInput, 0, firstInput.Length);
                target.GetHashAndReset();
                target.AppendData(Foobar, 0, Foobar.Length);
                var actual = target.GetHashAndReset();

                // Assert
                Assert.Equal(HashOfFoobar, actual);
            }
        }

        [Fact]
        public void CanBeRepeatedlyResetWithTheSameResut()
        {
            // Arrange
            using (var target = new MD5IncrementalHash())
            {
                // Act
                var first = target.GetHashAndReset();
                var second = target.GetHashAndReset();
                var third = target.GetHashAndReset();

                // Assert
                Assert.Equal(HashOfEmpty, first);
                Assert.Equal(HashOfEmpty, second);
                Assert.Equal(HashOfEmpty, third);
            }
        }
    }
}
