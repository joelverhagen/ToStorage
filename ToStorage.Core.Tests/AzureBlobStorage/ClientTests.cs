using System;
using System.IO;
using System.Net.Http;
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
        [Fact]
        public async Task Client_UploadsOverwritesExistingBlobs()
        {
            // Arrange
            var tc = new TestContext();
            tc.UploadRequest.ContentType = "application/json";
            tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("[1, 2]"));
            await tc.Client.UploadAsync(tc.ConnectionString, tc.UploadRequest);

            tc.UploadRequest.ContentType = "text/plain";
            tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));

            // Act
            var uploadResult = await tc.Client.UploadAsync(tc.ConnectionString, tc.UploadRequest);

            // Assert
            Assert.NotNull(uploadResult);
            await tc.VerifyUrlAsync(uploadResult.DirectUrl, "testpath/2015.01.02.03.04.05.txt");
            await tc.VerifyUrlAsync(uploadResult.LatestUrl, "testpath/latest.txt");
        }

        [Fact]
        public async Task Client_UploadsDirectAndLatestToStorage()
        {
            // Arrange
            var tc = new TestContext();

            // Act
            var uploadResult = await tc.Client.UploadAsync(tc.ConnectionString, tc.UploadRequest);

            // Assert
            Assert.NotNull(uploadResult);
            await tc.VerifyUrlAsync(uploadResult.DirectUrl, "testpath/2015.01.02.03.04.05.txt");
            await tc.VerifyUrlAsync(uploadResult.LatestUrl, "testpath/latest.txt");
        }

        [Fact]
        public async Task Client_UploadsJustDirectToStorage()
        {
            // Arrange
            var tc = new TestContext();
            tc.UploadRequest.UploadLatest = false;

            // Act
            var uploadResult = await tc.Client.UploadAsync(tc.ConnectionString, tc.UploadRequest);

            // Assert
            Assert.NotNull(uploadResult);
            await tc.VerifyUrlAsync(uploadResult.DirectUrl, "testpath/2015.01.02.03.04.05.txt");
            Assert.Null(uploadResult.LatestUrl);
        }

        [Fact]
        public async Task Client_UploadsJustLatestToStorage()
        {
            // Arrange
            var tc = new TestContext();
            tc.UploadRequest.UploadDirect = false;

            // Act
            var uploadResult = await tc.Client.UploadAsync(tc.ConnectionString, tc.UploadRequest);

            // Assert
            Assert.NotNull(uploadResult);
            Assert.Null(uploadResult.DirectUrl);
            await tc.VerifyUrlAsync(uploadResult.LatestUrl, "testpath/latest.txt");
        }

        [Fact]
        public async Task Client_DownloadsFromStorage()
        {
            // Arrange
            var tc = new TestContext();
            await tc.Client.UploadAsync(tc.ConnectionString, tc.UploadRequest);

            // Act
            using (var stream = await tc.Client.GetLatestStreamAsync(tc.ConnectionString, tc.GetLatestRequest))
            {
                // Assert
                using (var reader = new StreamReader(stream))
                {
                    Assert.Equal(tc.Content, reader.ReadToEnd());
                }
            }
        }

        private class TestContext
        {
            public TestContext()
            {
                // data
                UtcNow = new DateTimeOffset(2015, 1, 2, 3, 4, 5, TimeSpan.Zero);
                ConnectionString = "UseDevelopmentStorage=true";
                Content = "foobar";
                UploadRequest = new UploadRequest
                {
                    Container = "testcontainer",
                    ContentType = "text/plain",
                    PathFormat = Guid.NewGuid() + "/testpath/{0}.txt",
                    UploadDirect = true,
                    UploadLatest = true,
                    Stream = new MemoryStream(Encoding.UTF8.GetBytes(Content)),
                    Trace = TextWriter.Null
                };
                GetLatestRequest = new GetLatestRequest
                {
                    Container = UploadRequest.Container,
                    PathFormat = UploadRequest.PathFormat,
                    Trace = TextWriter.Null
                };

                // dependencies
                SystemTime = new Mock<ISystemTime>();

                // setup
                SystemTime.Setup(x => x.UtcNow).Returns(UtcNow);

                // target
                Client = new Client(SystemTime.Object);
            }

            public GetLatestRequest GetLatestRequest { get; }

            public string ConnectionString { get; }

            public string Content { get; }

            public UploadRequest UploadRequest { get; }

            public Client Client { get; }

            public Mock<ISystemTime> SystemTime { get; }

            public DateTimeOffset UtcNow { get; }

            public async Task<HttpResponseMessage> GetBlobAsync(Uri uri)
            {
                using (var httpClient = new HttpClient())
                {
                    return await httpClient.GetAsync(uri);
                }
            }

            public async Task VerifyUrlAsync(Uri uri, string endsWith)
            {
                Assert.NotNull(uri);
                Assert.Contains(UploadRequest.Container, uri.ToString());
                Assert.EndsWith(endsWith, uri.ToString());
                var response = await GetBlobAsync(uri);
                Assert.Equal(UploadRequest.ContentType, response.Content.Headers.ContentType.ToString());
                Assert.Equal(Content, await response.Content.ReadAsStringAsync());
            }
        }
    }
}
