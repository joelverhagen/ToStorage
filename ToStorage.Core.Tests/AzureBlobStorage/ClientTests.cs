using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Moq;
using Xunit;

namespace Knapcode.ToStorage.Core.Tests.AzureBlobStorage
{
    public class ClientTests
    {
        private const string ConnectionString = "UseDevelopmentStorage=true";

        [Fact]
        public async Task Client_UploadsToStorage()
        {
            // Arrange
            var systemTime = new Mock<ISystemTime>();
            var utcNow = new DateTimeOffset(2015, 1, 2, 3, 4, 5, TimeSpan.Zero);
            systemTime.Setup(x => x.UtcNow).Returns(utcNow);

            var client = new Client(systemTime.Object);
            var uploadRequest = new UploadRequest
            {
                Container = "testcontainer",
                ContentType = "text/plain",
                PathFormat = "testpath/{0}.txt",
                UploadDirect = true,
                UploadLatest = true,
                Stream = new MemoryStream(Encoding.ASCII.GetBytes("foobar")),
                Trace = TextWriter.Null
            };

            // Act
            var uploadResult = await client.UploadAsync(ConnectionString, uploadRequest);

            // Assert
            Assert.NotNull(uploadResult.DirectUrl);
            Assert.Contains(uploadRequest.Container, uploadResult.DirectUrl.ToString());
            Assert.EndsWith("testpath/2015.01.02.03.04.05.txt", uploadResult.DirectUrl.ToString());

            Assert.NotNull(uploadResult.LatestUrl);
            Assert.Contains(uploadRequest.Container, uploadResult.LatestUrl.ToString());
            Assert.EndsWith("testpath/latest.txt", uploadResult.LatestUrl.ToString());
        }
    }
}
