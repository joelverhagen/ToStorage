using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Microsoft.WindowsAzure.Storage;
using Moq;
using Xunit;

namespace Knapcode.ToStorage.Core.Tests.AzureBlobStorage
{
    public class ClientTests
    {
        [Fact]
        public async Task Client_GetsLatestUriResultWhenNotExisting()
        {
            // Arrange
            var tc = new TestContext();

            // Act
            var uriResult = await tc.Target.GetLatestUriResultAsync(tc.GetLatestRequest);

            // Assert
            Assert.Null(uriResult);
        }

        [Fact]
        public async Task Client_GetsLatestUriResultWhenExisting()
        {
            // Arrange
            var tc = new TestContext();
            await tc.Target.UploadAsync(tc.UploadRequest);

            // Act
            var uriResult = await tc.Target.GetLatestUriResultAsync(tc.GetLatestRequest);

            // Assert
            tc.VerifyUri(uriResult.Uri, "testpath/latest.txt");
            Assert.NotNull(uriResult.ETag);
        }

        [Fact]
        public void Client_GetsLatestUri()
        {
            // Arrange
            var tc = new TestContext();

            // Act
            var uri = tc.Target.GetLatestUri(tc.GetLatestRequest);

            // Assert
            tc.VerifyUri(uri, "testpath/latest.txt");
        }

        [Fact]
        public async Task Client_DoesNotOverwriteLatestWithDifferentETag()
        {
            // Arrange
            var tc = new TestContext();
            tc.Content = "[1, 2]";
            tc.UploadRequest.ContentType = "application/json";
            tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));
            var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

            tc.UploadRequest.ContentType = "text/plain";
            tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("foobar"));
            tc.UploadRequest.ETag = "bad etag";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<StorageException>(() => tc.Target.UploadAsync(tc.UploadRequest));
            Assert.Equal(412, exception.RequestInformation.HttpStatusCode);

            tc.UploadRequest.ContentType = "application/json";
            await tc.VerifyContentAsync(uploadResult.LatestUri);
        }

        [Fact]
        public async Task Client_UploadOverwritesExistingBlobs()
        {
            // Arrange
            var tc = new TestContext();
            tc.UploadRequest.ContentType = "application/json";
            tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("[1, 2]"));
            await tc.Target.UploadAsync(tc.UploadRequest);

            tc.UploadRequest.ContentType = "text/plain";
            tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));

            // Act
            var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

            // Assert
            Assert.NotNull(uploadResult);
            await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/2015.01.02.03.04.05.txt");
            Assert.NotNull(uploadResult.DirectETag);
            await tc.VerifyUriAndContentAsync(uploadResult.LatestUri, "testpath/latest.txt");
            Assert.NotNull(uploadResult.LatestETag);
        }

        [Fact]
        public async Task Client_UploadsDirectAndLatestToStorage()
        {
            // Arrange
            var tc = new TestContext();

            // Act
            var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

            // Assert
            Assert.NotNull(uploadResult);
            await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/2015.01.02.03.04.05.txt");
            Assert.NotNull(uploadResult.DirectETag);
            await tc.VerifyUriAndContentAsync(uploadResult.LatestUri, "testpath/latest.txt");
            Assert.NotNull(uploadResult.LatestETag);
        }

        [Fact]
        public async Task Client_UploadsJustDirectToStorage()
        {
            // Arrange
            var tc = new TestContext();
            tc.UploadRequest.UploadLatest = false;

            // Act
            var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

            // Assert
            Assert.NotNull(uploadResult);
            await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/2015.01.02.03.04.05.txt");
            Assert.NotNull(uploadResult.DirectETag);
            Assert.Null(uploadResult.LatestUri);
            Assert.Null(uploadResult.LatestETag);
        }

        [Fact]
        public async Task Client_UploadsJustLatestToStorage()
        {
            // Arrange
            var tc = new TestContext();
            tc.UploadRequest.UploadDirect = false;

            // Act
            var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

            // Assert
            Assert.NotNull(uploadResult);
            Assert.Null(uploadResult.DirectUri);
            Assert.Null(uploadResult.DirectETag);
            await tc.VerifyUriAndContentAsync(uploadResult.LatestUri, "testpath/latest.txt");
            Assert.NotNull(uploadResult.LatestETag);
        }

        [Fact]
        public async Task Client_DownloadsFromStorage()
        {
            // Arrange
            var tc = new TestContext();
            await tc.Target.UploadAsync(tc.UploadRequest);

            // Act
            using (var streamResult = await tc.Target.GetLatestStreamAsync(tc.GetLatestRequest))
            {
                // Assert
                Assert.NotNull(streamResult.ETag);
                using (var reader = new StreamReader(streamResult.Stream))
                {
                    Assert.Equal(tc.Content, reader.ReadToEnd());
                }
            }
        }

        [Fact]
        public async Task Client_ReturnsNullOnMissingLatest()
        {
            // Arrange
            var tc = new TestContext();
            await tc.Target.UploadAsync(tc.UploadRequest);
            tc.GetLatestRequest.PathFormat = "not-found/{0}.txt";

            // Act
            using (var stream = await tc.Target.GetLatestStreamAsync(tc.GetLatestRequest))
            {
                // Assert
                Assert.Null(stream);
            }
        }

        [Fact]
        public async Task Client_ReturnsNullOnMissingContainer()
        {
            // Arrange
            var tc = new TestContext();
            tc.GetLatestRequest.Container = "not-found-" + Guid.NewGuid();

            // Act
            using (var stream = await tc.Target.GetLatestStreamAsync(tc.GetLatestRequest))
            {
                // Assert
                Assert.Null(stream);
            }
        }

        private class TestContext
        {
            public TestContext()
            {
                // data
                UtcNow = new DateTimeOffset(2015, 1, 2, 3, 4, 5, TimeSpan.Zero);
                Content = "foobar";
                UploadRequest = new UploadRequest
                {
                    ConnectionString = "UseDevelopmentStorage=true",
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
                    ConnectionString = UploadRequest.ConnectionString,
                    Container = UploadRequest.Container,
                    PathFormat = UploadRequest.PathFormat,
                    Trace = TextWriter.Null
                };

                // dependencies
                SystemTime = new Mock<ISystemTime>();

                // setup
                SystemTime.Setup(x => x.UtcNow).Returns(UtcNow);

                // target
                Target = new Client(SystemTime.Object);
            }

            public GetLatestRequest GetLatestRequest { get; }

            public string ConnectionString { get; }

            public string Content { get; set; }

            public UploadRequest UploadRequest { get; }

            public Client Target { get; }

            public Mock<ISystemTime> SystemTime { get; }

            public DateTimeOffset UtcNow { get; }

            public async Task<HttpResponseMessage> GetBlobAsync(Uri uri)
            {
                using (var httpClient = new HttpClient())
                {
                    return await httpClient.GetAsync(uri);
                }
            }

            public async Task VerifyUriAndContentAsync(Uri uri, string endsWith)
            {
                VerifyUri(uri, endsWith);
                await VerifyContentAsync(uri);
            }

            public async Task VerifyContentAsync(Uri uri)
            {
                var response = await GetBlobAsync(uri);
                Assert.Equal(UploadRequest.ContentType, response.Content.Headers.ContentType.ToString());
                Assert.Equal(Content, await response.Content.ReadAsStringAsync());
            }

            public void VerifyUri(Uri url, string endsWith)
            {
                Assert.NotNull(url);
                Assert.Contains(UploadRequest.Container, url.ToString());
                Assert.EndsWith(endsWith, url.ToString());
            }
        }
    }
}
