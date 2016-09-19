using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Knapcode.Procommand;
using Knapcode.ToStorage.Core.Test;
using Xunit;
using Xunit.Abstractions;

namespace Knapcode.ToStorage.Tool.Test
{
    public class ProgramTests
    {
        private readonly ITestOutputHelper _output;

        public ProgramTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanUploadToDirectAndLatestWithShortOptions()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat
                    },
                    tc.Content);

                // Assert
                await tc.VerifyContentAsync(actual, direct: true, latest: true);
            }
        }

        [Fact]
        public async Task CanUploadToDirectAndLatestWithLongOptions()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "--connection-string", tc.ConnectionString,
                        "--container", tc.Container,
                        "--path-format", tc.PathFormat
                    },
                    tc.Content);

                // Assert
                await tc.VerifyContentAsync(actual, direct: true, latest: true);
            }
        }

        [Fact]
        public async Task CanUploadJustToDirectWithLongOptions()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "--connection-string", tc.ConnectionString,
                        "--container", tc.Container,
                        "--path-format", tc.PathFormat,
                        "--no-latest"
                    },
                    tc.Content);

                // Assert
                await tc.VerifyContentAsync(actual, direct: true, latest: false);
            }
        }

        [Fact]
        public async Task CanUploadJustToLatestWithLongOptions()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "--connection-string", tc.ConnectionString,
                        "--container", tc.Container,
                        "--path-format", tc.PathFormat,
                        "--no-direct"
                    },
                    tc.Content);

                // Assert
                await tc.VerifyContentAsync(actual, direct: false, latest: true);
            }
        }

        [Fact]
        public void CanUploadOnlyWhenUniqueWithShortOptions()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                var initial = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat
                    },
                    tc.Content);

                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat,
                        "-u", "true"
                    },
                    tc.Content);

                // Assert
                tc.VerifyCommandResult(actual);
                Assert.Equal("Gettings the existing latest... exactly the same! No upload required.", actual.Output.TrimEnd());
            }
        }

        [Fact]
        public void CanUploadOnlyWhenUniqueWithLongOptions()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                var initial = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat
                    },
                    tc.Content);

                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "--connection-string", tc.ConnectionString,
                        "--container", tc.Container,
                        "--path-format", tc.PathFormat,
                        "--only-unique"
                    },
                    tc.Content);

                // Assert
                tc.VerifyCommandResult(actual);
                Assert.Equal("Gettings the existing latest... exactly the same! No upload required.", actual.Output.TrimEnd());
            }
        }

        [Fact]
        public async Task CanUploadOnlyLatestWithUniqueOnlyOption()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                tc.Content = "something";
                var initial = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat,
                        "--no-direct"
                    },
                    tc.Content);

                tc.Content = "something else";

                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "--connection-string", tc.ConnectionString,
                        "--container", tc.Container,
                        "--path-format", tc.PathFormat,
                        "--no-direct",
                        "--only-unique"
                    },
                    tc.Content);

                // Assert
                await tc.VerifyContentAsync(actual, direct: false, latest: true);
                Assert.Contains("Gettings the existing latest... different! The provided content will be uploaded.", actual.Output);
            }
        }

        [Fact]
        public async Task CanSetContentTypeWithShortOptions()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                tc.Content = "{\"foo\": 5}";
                tc.ContentType = "application/json";

                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat,
                        "-t", tc.ContentType
                    },
                    tc.Content);

                // Assert
                await tc.VerifyContentAsync(actual, direct: true, latest: true);
            }
        }

        [Fact]
        public async Task CanSetContentTypeWithLongOptions()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                tc.Content = "{\"foo\": 5}";
                tc.ContentType = "application/json";

                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "--connection-string", tc.ConnectionString,
                        "--container", tc.Container,
                        "--path-format", tc.PathFormat,
                        "--content-type", tc.ContentType
                    },
                    tc.Content);

                // Assert
                await tc.VerifyContentAsync(actual, direct: true, latest: true);
            }
        }

        [Fact]
        public async Task AllowsBothDirectAndLatestUploadToBeDisabled()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat,
                        "--no-direct",
                        "--no-latest"
                    },
                    tc.Content);

                // Assert
                await tc.VerifyContentAsync(actual, direct: false, latest: false);
            }
        }

        [Fact]
        public async Task HasDefaultPathFormat()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                var minTimestamp = DateTimeOffset.UtcNow;

                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container
                    },
                    tc.Content);

                // Assert
                var maxTimestamp = DateTimeOffset.UtcNow;

                var result = await tc.VerifyContentAsync(actual, direct: true, latest: true);

                var directFileName = result.DirectUri.ToString().Split('/').Last();
                Assert.EndsWith(".txt", directFileName);
                var unparsedTimestamp = directFileName.Substring(0, directFileName.Length - ".txt".Length);
                var timestampLocal = DateTime.ParseExact(unparsedTimestamp, "yyyy.MM.dd.HH.mm.ss.fffffff", CultureInfo.InvariantCulture);
                var timestamp = new DateTimeOffset(timestampLocal, TimeSpan.Zero);
                Assert.True(timestamp >= minTimestamp, "The timestamp should be greater than or equal to the minimum.");
                Assert.True(timestamp <= maxTimestamp, "The timestamp should be less than or equal to the maximum.");

                Assert.EndsWith("/latest.txt", result.LatestUri.ToString());
            }
        }

        [Fact]
        public void RequiresConnectionStringOption()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "--container", tc.Container,
                        "--path-format", tc.PathFormat
                    },
                    tc.Content);

                // Assert
                Assert.Equal(CommandStatus.Exited, actual.Status);
                Assert.Equal(1, actual.ExitCode);
                Assert.Contains("Required option 's, connection-string' is missing.", actual.Error);
            }
        }

        [Fact]
        public void RequiresContainerOption()
        {
            // Arrange
            using (var tc = new TestContext(_output))
            {
                // Act
                var actual = tc.ExecuteCommand(
                    new[]
                    {
                        "--connection-string", tc.ConnectionString,
                        "--path-format", tc.PathFormat
                    },
                    tc.Content);

                // Assert
                Assert.Equal(CommandStatus.Exited, actual.Status);
                Assert.Equal(1, actual.ExitCode);
                Assert.Contains("Required option 'c, container' is missing.", actual.Error);
            }
        }

        private class TestContext : IDisposable
        {
            public TestContext(ITestOutputHelper output)
            {
                // data
                Output = output;
                Content = "First line." + Environment.NewLine +
                          "Second line." + Environment.NewLine +
                          "Unique thing: " + Guid.NewGuid();
                ContentType = "text/plain";
                Prefix = "testpath";
                ConnectionString = TestSupport.ConnectionString;
                Container = TestSupport.GetTestContainer();
                ToolPath = GetToolPath();
            }

            public CommandResult ExecuteCommand(IEnumerable<string> arguments, string stdin)
            {
                var runner = new CommandRunner();
                var command = new Command(ToolPath, arguments)
                {
                    Input = new MemoryStream(Encoding.UTF8.GetBytes(stdin))
                };

                return runner.Run(command);
            }

            private async Task<ContentAndContentType> GetContentAsync(string requestUri)
            {
                using (var httpClient = new HttpClient())
                using (var response = await httpClient.GetAsync(requestUri))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var contentType = response.Content.Headers.ContentType.ToString();

                    return new ContentAndContentType
                    {
                        Content = content,
                        ContentType = contentType
                    };
                }
            }

            public async Task<UploadResult> VerifyContentAsync(CommandResult commandResult, bool direct, bool latest)
            {
                VerifyCommandResult(commandResult);

                var uploadResult = new UploadResult();
                if (direct)
                {
                    var uri = await VerifyLineUriAsync(commandResult, "Direct: ");
                    uploadResult.DirectUri = uri;
                }
                else
                {
                    VerifyNoLine(commandResult, "Direct: ");
                }

                if (latest)
                {
                    var uri = await VerifyLineUriAsync(commandResult, "Latest: ");
                    uploadResult.LatestUri = uri;
                }
                else
                {
                    VerifyNoLine(commandResult, "Latest: ");
                }

                return uploadResult;
            }

            public void VerifyCommandResult(CommandResult commandResult)
            {
                try
                {
                    Assert.Equal(CommandStatus.Exited, commandResult.Status);
                    Assert.Equal(0, commandResult.ExitCode);
                    Assert.Empty(commandResult.Error);
                }
                catch
                {
                    Output.WriteLine($"File name: {commandResult.Command.FileName}");
                    Output.WriteLine($"Arguments: {commandResult.Command.Arguments}");
                    Output.WriteLine($"Working directory: {commandResult.Command.WorkingDirectory}");
                    Output.WriteLine($"Status: {commandResult.Status}");
                    Output.WriteLine($"Exit code: {commandResult.ExitCode}");
                    Output.WriteLine($"Exception:{Environment.NewLine}{commandResult.Exception}");
                    Output.WriteLine($"stdout:{Environment.NewLine}{commandResult.Output}");
                    Output.WriteLine($"stderr:{Environment.NewLine}{commandResult.Error}");
                }
            }

            private async Task<Uri> VerifyLineUriAsync(CommandResult commandResult, string linePrefix)
            {
                var line = commandResult.OutputLines.FirstOrDefault(l => l != null && l.StartsWith(linePrefix));
                Assert.NotNull(line);

                var uri = line.Split(new[] { ' ' }, 2)[1];
                var actualContent = await GetContentAsync(uri);

                Assert.Equal(Content, actualContent.Content);
                Assert.Equal(ContentType, actualContent.ContentType);

                return new Uri(uri);
            }

            private void VerifyNoLine(CommandResult commandResult, string linePrefix)
            {
                var line = commandResult.OutputLines.FirstOrDefault(l => l != null && l.StartsWith(linePrefix));
                Assert.Null(line);
            }

            private static string GetToolPath()
            {
                var repositoryRoot = GetRepositoryRoot();

                return Path.Combine(
                    repositoryRoot,
                    "src",
                    "Knapcode.ToStorage.Tool",
                    "bin",
#if DEBUG
                    "Debug",
#else
                    "Release",
#endif
                    "net45",
                    "win7-x64",
                    "publish",
                    "ToStorage.exe");
            }

            private static string GetRepositoryRoot()
            {
                var current = Directory.GetCurrentDirectory();
                while (current != null &&
                       !Directory.GetFiles(current).Any(x => Path.GetFileName(x) == "ToStorage.sln"))
                {
                    current = Path.GetDirectoryName(current);
                }

                return current;
            }

            public string Content { get; set; }
            public string Prefix { get; }
            public string ConnectionString { get; }
            public string Container { get; }
            public string ToolPath { get; }
            public string PathFormat => $"{Prefix}/{{0}}.txt";
            public string ContentType { get; set; }
            public ITestOutputHelper Output { get; }

            public void Dispose()
            {
                TestSupport.DeleteContainer(Container);
            }
        }

        private class ContentAndContentType
        {
            public string Content { get; set; }
            public string ContentType { get; set; }
        }

        private class UploadResult
        {
            public Uri DirectUri { get; set; }
            public Uri LatestUri { get; set; }
        }
    }
}
