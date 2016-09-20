using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Xunit;

namespace Knapcode.ToStorage.Core.Test.AzureBlobStorage
{
    public class CollapserTests
    {
        [Fact]
        public async Task Collapser_CollapsesLotsOfBlobs()
        {
            // Arrange
            using (var tc = new TestContext())
            {
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
                var blobs = await tc.ListBlobsAsync();
                Assert.Contains("2015.01.02.03.04.05", blobs[0].Name);
                Assert.Contains("2015.01.02.03.04.07", blobs[1].Name);
                Assert.Contains("2015.01.02.03.04.10", blobs[2].Name);
                Assert.Contains("2015.01.02.03.04.11", blobs[3].Name);
                Assert.Contains("2015.01.02.03.04.13", blobs[4].Name);
                Assert.Contains("latest", blobs[5].Name);
                Assert.Equal("a", blobs[0].Content);
                Assert.Equal("b", blobs[1].Content);
                Assert.Equal("a", blobs[2].Content);
                Assert.Equal("c", blobs[3].Content);
                Assert.Equal("d", blobs[4].Content);
                Assert.Equal("d", blobs[5].Content);
                Assert.Equal(6, blobs.Count);
            }
        }

        [Fact]
        public async Task Collapser_CollapsesTwoBlobs()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                await tc.UploadAsync("a");
                await tc.UploadAsync("a");

                // Act
                await tc.Target.CollapseAsync(tc.CollapseRequest);

                // Assert
                var blobs = await tc.ListBlobsAsync();
                Assert.Contains("2015.01.02.03.04.05", blobs[0].Name);
                Assert.Contains("latest", blobs[1].Name);
                Assert.Equal("a", blobs[0].Content);
                Assert.Equal("a", blobs[1].Content);
                Assert.Equal(2, blobs.Count);
            }
        }

        [Fact]
        public async Task Collapser_LeavesOneBlobAlone()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                await tc.UploadAsync("a");

                // Act
                await tc.Target.CollapseAsync(tc.CollapseRequest);

                // Assert
                var blobs = await tc.ListBlobsAsync();
                Assert.Contains("2015.01.02.03.04.05", blobs[0].Name);
                Assert.Contains("latest", blobs[1].Name);
                Assert.Equal("a", blobs[0].Content);
                Assert.Equal("a", blobs[1].Content);
                Assert.Equal(2, blobs.Count);
            }
        }

        [Fact]
        public async Task Collapser_AllowsNoBlobs()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                // Act
                await tc.Target.CollapseAsync(tc.CollapseRequest);

                // Assert
                Assert.False(await tc.CloudContext.BlobContainer.ExistsAsync());
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
                Prefix = "testpath";
                UploadRequest = new UploadRequest
                {
                    ConnectionString = TestSupport.ConnectionString,
                    Container = Container,
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
                    Comparer = new OrdinalCollapserComparer(),
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
            public string Container { get; }

            public async Task UploadAsync(string content)
            {
                UploadRequest.Stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                await Client.UploadAsync(UploadRequest);
            }

            public async Task<string> GetStringAsync(ICloudBlob blob)
            {
                using (var stream = await blob.OpenReadAsync(accessCondition: null, options: null, operationContext: null))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }

            public async Task<List<Blob>> ListBlobsAsync()
            {
                var results = new List<Blob>();

                BlobContinuationToken continuationToken = null;
                var more = true;
                while (more)
                {
                    var segment = await CloudContext.BlobContainer.ListBlobsSegmentedAsync(
                        Prefix,
                        useFlatBlobListing: true,
                        blobListingDetails: BlobListingDetails.All,
                        maxResults: null,
                        currentToken: continuationToken,
                        options: null,
                        operationContext: null);

                    continuationToken = segment.ContinuationToken;

                    var contents = await Task.WhenAll(segment
                        .Results
                        .OfType<ICloudBlob>()
                        .Select(x => GetStringAsync(x)));

                    var segmentBlobs = segment
                        .Results
                        .OfType<ICloudBlob>()
                        .Zip(contents, (blob, c) => new Blob { Name = blob.Name, Content = c })
                        .ToList();

                    more = continuationToken != null;

                    results.AddRange(segmentBlobs);
                }

                return results;
            }

            public void Dispose()
            {
                TestSupport.DeleteContainer(Container);
            }
        }

        private class Blob
        {
            public string Name { get; set; }
            public string Content { get; set; }
        }
    }
}
