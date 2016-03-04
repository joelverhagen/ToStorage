using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Xunit;

namespace Knapcode.ToStorage.Core.Tests.AzureBlobStorage
{
    public class CollapserTests
    {
        [Fact]
        public async Task Collapser_CollapsesLotsOfBlobs()
        {
            // Arrange
            var tc = new TestContext();
            await tc.UploadAsync("a");
            await tc.UploadAsync("a");
            await tc.UploadAsync("b");
            await tc.UploadAsync("b");
            await tc.UploadAsync("b");
            await tc.UploadAsync("a");
            await tc.UploadAsync("c");
            await tc.UploadAsync("c");
            await tc.UploadAsync("d");
            await tc.UploadAsync("d");

            // Act
            await tc.Target.CollapseAsync(tc.CollapseRequest);

            // Assert
            var blobs = tc.CloudContext.BlobContainer.ListBlobs(tc.Prefix, true);
            var blobContent = blobs
                .OfType<ICloudBlob>()
                .Select(x => new { x.Name, Content = tc.GetString(x)})
                .OrderBy(x => x.Name)
                .ToArray();
            Assert.Contains("2015.01.02.03.04.05", blobContent[0].Name);
            Assert.Contains("2015.01.02.03.04.07", blobContent[1].Name);
            Assert.Contains("2015.01.02.03.04.10", blobContent[2].Name);
            Assert.Contains("2015.01.02.03.04.11", blobContent[3].Name);
            Assert.Contains("2015.01.02.03.04.13", blobContent[4].Name);
            Assert.Contains("latest", blobContent[5].Name);
            Assert.Equal("a", blobContent[0].Content);
            Assert.Equal("b", blobContent[1].Content);
            Assert.Equal("a", blobContent[2].Content);
            Assert.Equal("c", blobContent[3].Content);
            Assert.Equal("d", blobContent[4].Content);
            Assert.Equal("d", blobContent[5].Content);
            Assert.Equal(6, blobContent.Length);
        }

        [Fact]
        public async Task Collapser_CollapsesTwoBlobs()
        {
            // Arrange
            var tc = new TestContext();
            await tc.UploadAsync("a");
            await tc.UploadAsync("a");

            // Act
            await tc.Target.CollapseAsync(tc.CollapseRequest);

            // Assert
            var blobs = tc.CloudContext.BlobContainer.ListBlobs(tc.Prefix, true);
            var blobContent = blobs
                .OfType<ICloudBlob>()
                .Select(x => new { x.Name, Content = tc.GetString(x) })
                .OrderBy(x => x.Name)
                .ToArray();
            Assert.Contains("2015.01.02.03.04.05", blobContent[0].Name);
            Assert.Contains("latest", blobContent[1].Name);
            Assert.Equal("a", blobContent[0].Content);
            Assert.Equal("a", blobContent[1].Content);
            Assert.Equal(2, blobContent.Length);
        }

        [Fact]
        public async Task Collapser_LeavesOneBlobAlone()
        {
            // Arrange
            var tc = new TestContext();
            await tc.UploadAsync("a");

            // Act
            await tc.Target.CollapseAsync(tc.CollapseRequest);

            // Assert
            var blobs = tc.CloudContext.BlobContainer.ListBlobs(tc.Prefix, true);
            var blobContent = blobs
                .OfType<ICloudBlob>()
                .Select(x => new { x.Name, Content = tc.GetString(x) })
                .ToArray();
            Assert.Contains("2015.01.02.03.04.05", blobContent[0].Name);
            Assert.Contains("latest", blobContent[1].Name);
            Assert.Equal("a", blobContent[0].Content);
            Assert.Equal("a", blobContent[1].Content);
            Assert.Equal(2, blobContent.Length);
        }

        [Fact]
        public async Task Collapser_AllowsNoBlobs()
        {
            // Arrange
            var tc = new TestContext();

            // Act
            await tc.Target.CollapseAsync(tc.CollapseRequest);

            // Assert
            var blobs = tc.CloudContext.BlobContainer.ListBlobs(tc.Prefix, true);
            Assert.Equal(0, blobs.Count());
        }

        private class TestContext
        {
            public TestContext()
            {
                // data
                UtcNow = new DateTimeOffset(2015, 1, 2, 3, 4, 5, TimeSpan.Zero);
                Content = "foobar";
                Prefix = Guid.NewGuid() + "/testpath";
                UploadRequest = new UploadRequest
                {
                    ConnectionString = "UseDevelopmentStorage=true",
                    Container = "testcontainer",
                    ContentType = "text/plain",
                    PathFormat = Prefix + "/{0}.txt",
                    UploadDirect = true,
                    UploadLatest = true,
                    Stream = new MemoryStream(Encoding.UTF8.GetBytes(Content)),
                    Trace = TextWriter.Null
                };
                CollapseRequest = new CollapseRequest
                {
                    ConnectionString = UploadRequest.ConnectionString,
                    Container = UploadRequest.Container,
                    PathFormat = UploadRequest.PathFormat,
                    Comparer = new CollapserComparer(),
                    Trace = TextWriter.Null
                };
                CloudContext = new CloudContext(UploadRequest.ConnectionString, UploadRequest.Container);

                // dependencies
                SystemTime = new Mock<ISystemTime>();
                PathBuilder = new PathBuilder();
                Client = new Client(SystemTime.Object, PathBuilder);

                // setup
                SystemTime
                    .Setup(x => x.UtcNow)
                    .Returns(() => UtcNow).Callback(() => UtcNow = UtcNow.AddSeconds(1));

                // target
                Target = new Collapser(PathBuilder);
            }

            public string Prefix { get; }

            public CloudContext CloudContext { get; }

            public PathBuilder PathBuilder { get; }

            public DateTimeOffset UtcNow { get; set; }

            public CollapseRequest CollapseRequest { get; }

            public Collapser Target { get; }

            public string Content { get; }

            public UploadRequest UploadRequest { get; }

            public Client Client { get; }

            public Mock<ISystemTime> SystemTime { get; }

            public async Task UploadAsync(string content)
            {
                UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                await Client.UploadAsync(UploadRequest);
            }

            public string GetString(ICloudBlob blob)
            {
                using (var stream = blob.OpenRead())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
