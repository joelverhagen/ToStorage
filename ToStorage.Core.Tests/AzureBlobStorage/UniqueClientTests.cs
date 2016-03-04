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
    public class UniqueClientTests
    {
        [Fact]
        public async Task UniqueClient_UpdatesUniqueWithoutExisting()
        {
            // Arrange
            var tc = new TestContext();

            // Act
            var actual = await tc.Target.UploadAsync(tc.UniqueUploadRequest);

            // Assert
            await tc.VerifyContentAsync(actual.DirectUri);
            await tc.VerifyContentAsync(actual.LatestUri);
        }

        [Fact]
        public async Task UniqueClient_UpdatesUniqueWithExisting()
        {
            // Arrange
            var tc = new TestContext();
            tc.UniqueUploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes("oldContent"));
            await tc.Target.UploadAsync(tc.UniqueUploadRequest);
            tc.UniqueUploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));

            // Act
            var actual = await tc.Target.UploadAsync(tc.UniqueUploadRequest);

            // Assert
            await tc.VerifyContentAsync(actual.DirectUri);
            await tc.VerifyContentAsync(actual.LatestUri);
        }

        [Fact]
        public async Task UniqueClient_DoesNotUpdateNonUniqueContent()
        {
            // Arrange
            var tc = new TestContext();
            await tc.Target.UploadAsync(tc.UniqueUploadRequest);
            tc.UniqueUploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(tc.Content));

            // Act
            var actual = await tc.Target.UploadAsync(tc.UniqueUploadRequest);

            // Assert
            Assert.Null(actual);
        }

        private class TestContext
        {
            public TestContext()
            {
                // data
                UtcNow = new DateTimeOffset(2015, 1, 2, 3, 4, 5, TimeSpan.Zero);
                Content = "newContent";
                UniqueUploadRequest = new UniqueUploadRequest
                {
                    ConnectionString = "UseDevelopmentStorage=true",
                    Container = "testcontainer",
                    ContentType = "text/plain",
                    PathFormat = Guid.NewGuid() + "/testpath/{0}.txt",
                    UploadDirect = true,
                    Stream = new MemoryStream(Encoding.UTF8.GetBytes(Content)),
                    Trace = TextWriter.Null,
                    EqualsAsync = async x => (await new StreamReader(x.Stream).ReadLineAsync()) == Content
                };
                GetLatestRequest = new GetLatestRequest
                {
                    ConnectionString = UniqueUploadRequest.ConnectionString,
                    Container = UniqueUploadRequest.Container,
                    PathFormat = UniqueUploadRequest.PathFormat,
                    Trace = TextWriter.Null
                };

                // dependencies
                SystemTime = new Mock<ISystemTime>();
                PathBuilder = new PathBuilder();
                Client = new Client(SystemTime.Object, PathBuilder);

                // setup
                SystemTime.Setup(x => x.UtcNow).Returns(UtcNow);

                // target
                Target = new UniqueClient(Client);
            }

            public PathBuilder PathBuilder { get; }

            public string Content { get; }

            public UniqueClient Target { get; }

            public GetLatestRequest GetLatestRequest { get; }
            
            public UniqueUploadRequest UniqueUploadRequest { get; }

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

            public async Task VerifyContentAsync(Uri uri)
            {
                var response = await GetBlobAsync(uri);
                Assert.Equal(UniqueUploadRequest.ContentType, response.Content.Headers.ContentType.ToString());
                Assert.Equal(Content, await response.Content.ReadAsStringAsync());
            }
        }
    }
}
