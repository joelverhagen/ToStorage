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

namespace Knapcode.ToStorage.Core.Test.AzureBlobStorage
{
    public class ClientTests
    {
        [Fact]
        public async Task Client_GetsLatestUriResultWhenNotExisting()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                // Act
                var uriResult = await tc.Target.GetLatestUriResultAsync(tc.GetLatestRequest);

                // Assert
                Assert.Null(uriResult);
            }
        }

        [Fact]
        public async Task Client_GetsLatestUriResultWhenExisting()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                await tc.Target.UploadAsync(tc.UploadRequest);

                // Act
                var uriResult = await tc.Target.GetLatestUriResultAsync(tc.GetLatestRequest);

                // Assert
                tc.VerifyUri(uriResult.Uri, "testpath/latest.txt");
                Assert.NotNull(uriResult.ETag);
            }
        }

        [Fact]
        public void Client_GetsLatestUri()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                // Act
                var uri = tc.Target.GetLatestUri(tc.GetLatestRequest);

                // Assert
                tc.VerifyUri(uri, "testpath/latest.txt");
            }
        }

        [Fact]
        public async Task Client_DoesNotOverwriteExistingDirectBlob()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.Content = "[1, 2]";
                tc.UploadRequest.ContentType = "application/json";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                tc.UploadRequest.ContentType = "text/plain";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("foobar"));
                tc.UploadRequest.ETag = uploadResult.LatestETag;

                // Act & Assert
                var exception = await Assert.ThrowsAsync<StorageException>(() => tc.Target.UploadAsync(tc.UploadRequest));
                Assert.Equal(409, exception.RequestInformation.HttpStatusCode);

                tc.UploadRequest.ContentType = "application/json";
                await tc.VerifyContentAsync(uploadResult.LatestUri);
            }
        }

        [Fact]
        public async Task Client_DoesNotOverwriteLatestWithDifferentETag()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.Content = "[1, 2]";
                tc.UploadRequest.ContentType = "application/json";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                tc.UploadRequest.ContentType = "text/plain";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("foobar"));
                tc.UploadRequest.ETag = "\"bad etag\"";

                tc.UtcNow = tc.UtcNow.AddMinutes(1);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<StorageException>(() => tc.Target.UploadAsync(tc.UploadRequest));
                Assert.Equal(412, exception.RequestInformation.HttpStatusCode);

                tc.UploadRequest.ContentType = "application/json";
                await tc.VerifyContentAsync(uploadResult.LatestUri);
            }
        }

        [Fact]
        public async Task Client_UploadOverwritesExistingTimestampBlobs()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.UploadRequest.Type = UploadRequestType.Timestamp;
                tc.UploadRequest.ContentType = "application/json";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("[1, 2]"));
                var setupResult = await tc.Target.UploadAsync(tc.UploadRequest);

                tc.UploadRequest.ContentType = "text/plain";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));
                tc.UploadRequest.ETag = setupResult.LatestETag;

                tc.UtcNow = tc.UtcNow.AddMinutes(1);

                // Act
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                // Assert
                Assert.NotNull(uploadResult);
                await tc.VerifyUriAndContentAsync(setupResult.DirectUri, "testpath/2015.01.02.03.04.05.0060000.txt", "[1, 2]");
                await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/2015.01.02.03.05.05.0060000.txt");
                Assert.NotNull(uploadResult.DirectETag);
                await tc.VerifyUriAndContentAsync(uploadResult.LatestUri, "testpath/latest.txt");
                Assert.NotNull(uploadResult.LatestETag);
            }
        }

        [Fact]
        public async Task Client_UploadNumberOverwritesLatestBlob()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.UploadRequest.Type = UploadRequestType.Number;
                tc.UploadRequest.ContentType = "application/json";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("[1, 2]"));
                var setupResult = await tc.Target.UploadAsync(tc.UploadRequest);

                tc.UploadRequest.ETag = setupResult.LatestETag;
                tc.UploadRequest.ContentType = "text/plain";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));

                // Act
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                // Assert
                Assert.NotNull(uploadResult);
                await tc.VerifyUriAndContentAsync(setupResult.DirectUri, "testpath/1.txt", "[1, 2]");
                await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/2.txt");
                Assert.NotNull(uploadResult.DirectETag);
                await tc.VerifyUriAndContentAsync(uploadResult.LatestUri, "testpath/latest.txt");
                Assert.NotNull(uploadResult.LatestETag);
                Assert.NotNull(uploadResult.LatestNumberETag);
                Assert.Equal(2, uploadResult.LatestNumber);
            }
        }

        [Fact]
        public async Task Client_AllowsNextNumberToBeChangedWithRequest()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.UploadRequest.Type = UploadRequestType.Number;
                tc.UploadRequest.ContentType = "application/json";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("[1, 2]"));
                var setupResult = await tc.Target.UploadAsync(tc.UploadRequest);

                tc.UploadRequest.LatestNumberETag = setupResult.LatestNumberETag;
                tc.UploadRequest.LatestNumber = 5;
                tc.UploadRequest.ETag = setupResult.LatestETag;
                tc.UploadRequest.ContentType = "text/plain";
                tc.UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));

                // Act
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                // Assert
                Assert.NotNull(uploadResult);
                await tc.VerifyUriAndContentAsync(setupResult.DirectUri, "testpath/1.txt", "[1, 2]");
                await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/5.txt");
                Assert.NotNull(uploadResult.DirectETag);
                await tc.VerifyUriAndContentAsync(uploadResult.LatestUri, "testpath/latest.txt");
                Assert.NotNull(uploadResult.LatestETag);
                Assert.NotNull(uploadResult.LatestNumberETag);
                Assert.Equal(5, uploadResult.LatestNumber);
            }
        }

        [Fact]
        public async Task Client_UploadsDirectTimestampAndLatestToStorage()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.UploadRequest.UploadLatest = true;
                tc.UploadRequest.Type = UploadRequestType.Timestamp;

                // Act
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                // Assert
                Assert.NotNull(uploadResult);
                await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/2015.01.02.03.04.05.0060000.txt");
                Assert.NotNull(uploadResult.DirectETag);
                await tc.VerifyUriAndContentAsync(uploadResult.LatestUri, "testpath/latest.txt");
                Assert.NotNull(uploadResult.LatestETag);
            }
        }

        [Fact]
        public async Task Client_UploadsDirectNumberAndLatestToStorage()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.UploadRequest.UploadLatest = true;
                tc.UploadRequest.Type = UploadRequestType.Number;

                // Act
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                // Assert
                Assert.NotNull(uploadResult);
                await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/1.txt");
                Assert.NotNull(uploadResult.DirectETag);
                await tc.VerifyUriAndContentAsync(uploadResult.LatestUri, "testpath/latest.txt");
                Assert.NotNull(uploadResult.LatestETag);
                Assert.NotNull(uploadResult.LatestNumberETag);
                Assert.Equal(1, uploadResult.LatestNumber);
            }
        }

        [Fact]
        public async Task Client_UploadsDirectNumberToStorage()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.UploadRequest.Type = UploadRequestType.Number;
                tc.UploadRequest.UploadLatest = false;

                // Act
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                // Assert
                Assert.NotNull(uploadResult);
                await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/1.txt");
                Assert.NotNull(uploadResult.DirectETag);
                Assert.Null(uploadResult.LatestUri);
                Assert.Null(uploadResult.LatestETag);
                Assert.NotNull(uploadResult.LatestNumberETag);
                Assert.Equal(1, uploadResult.LatestNumber);
            }
        }

        [Fact]
        public async Task Client_UploadsJustDirectTimestampToStorage()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.UploadRequest.Type = UploadRequestType.Timestamp;
                tc.UploadRequest.UploadLatest = false;

                // Act
                var uploadResult = await tc.Target.UploadAsync(tc.UploadRequest);

                // Assert
                Assert.NotNull(uploadResult);
                await tc.VerifyUriAndContentAsync(uploadResult.DirectUri, "testpath/2015.01.02.03.04.05.0060000.txt");
                Assert.NotNull(uploadResult.DirectETag);
                Assert.Null(uploadResult.LatestUri);
                Assert.Null(uploadResult.LatestETag);
            }
        }

        [Fact]
        public async Task Client_UploadsJustLatestToStorage()
        {
            // Arrange
            using (var tc = new TestContext())
            {
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
        }

        [Fact]
        public async Task Client_DownloadsFromStorage()
        {
            // Arrange
            using (var tc = new TestContext())
            {
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
        }

        [Fact]
        public async Task Client_ReturnsNullOnMissingLatest()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                await tc.Target.UploadAsync(tc.UploadRequest);
                tc.GetLatestRequest.PathFormat = "not-found/{0}.txt";

                // Act
                using (var stream = await tc.Target.GetLatestStreamAsync(tc.GetLatestRequest))
                {
                    // Assert
                    Assert.Null(stream);
                }
            }
        }

        [Fact]
        public async Task Client_ReturnsNullOnMissingContainer()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                tc.GetLatestRequest.Container = "not-found-" + Guid.NewGuid();

                // Act
                using (var stream = await tc.Target.GetLatestStreamAsync(tc.GetLatestRequest))
                {
                    // Assert
                    Assert.Null(stream);
                }
            }
        }

        private class TestContext : IDisposable
        {
            public TestContext()
            {
                // data
                UtcNow = new DateTimeOffset(2015, 1, 2, 3, 4, 5, 6, TimeSpan.Zero);
                Content = "foobar";
                Container = TestSupport.GetTestContainer();
                UploadRequest = new UploadRequest
                {
                    ConnectionString = TestSupport.ConnectionString,
                    Container = Container,
                    ContentType = "text/plain",
                    PathFormat = "testpath/{0}.txt",
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
                PathBuilder = new PathBuilder();

                // setup
                SystemTime.Setup(x => x.UtcNow).Returns(() => UtcNow);

                // target
                Target = new Client(SystemTime.Object, PathBuilder);
            }

            public PathBuilder PathBuilder { get; }

            public GetLatestRequest GetLatestRequest { get; }

            public string Content { get; set; }

            public UploadRequest UploadRequest { get; }

            public Client Target { get; }

            public Mock<ISystemTime> SystemTime { get; }

            public DateTimeOffset UtcNow { get; set; }
            public string Container { get; }

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

            public async Task VerifyUriAndContentAsync(Uri uri, string endsWith, string content)
            {
                VerifyUri(uri, endsWith);
                await VerifyContentAsync(uri, content);
            }

            public async Task VerifyContentAsync(Uri uri, string content)
            {
                var response = await GetBlobAsync(uri);
                Assert.Equal(content, await response.Content.ReadAsStringAsync());
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

            public void Dispose()
            {
                TestSupport.DeleteContainer(Container);
            }
        }
    }
}
