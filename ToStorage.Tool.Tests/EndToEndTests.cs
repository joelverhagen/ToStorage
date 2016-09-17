using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Knapcode.Procommand;
using Knapcode.ToStorage.Core.Tests;
using Xunit;

namespace Knapcode.ToStorage.Tool.Tests
{
    public class EndToEndTests
    {
        [Fact]
        public async Task StdinCanBeUploadedToDirectAndLatest()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                // Act
                var result = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat
                    },
                    tc.Content);

                // Assert
                await tc.VerifyCommandResult(result, direct: true, latest: true);
            }
        }

        [Fact]
        public async Task StdinCanBeUploadedJustToDirect()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                // Act
                var result = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat,
                        "-l", "false"
                    },
                    tc.Content);

                // Assert
                await tc.VerifyCommandResult(result, direct: true, latest: false);
            }
        }

        [Fact]
        public async Task StdinCanBeUploadedJustToLatest()
        {
            // Arrange
            using (var tc = new TestContext())
            {
                // Act
                var result = tc.ExecuteCommand(
                    new[]
                    {
                        "-s", tc.ConnectionString,
                        "-c", tc.Container,
                        "-f", tc.PathFormat,
                        "-d", "false"
                    },
                    tc.Content);

                // Assert
                await tc.VerifyCommandResult(result, direct: false, latest: true);
            }
        }

        private class TestContext : IDisposable
        {
            public TestContext()
            {
                // data
                Content = "First line." + Environment.NewLine +
                          "Second line." + Environment.NewLine +
                          "Unique thing: " + Guid.NewGuid();
                Prefix = Guid.NewGuid() + "/testpath";
                ConnectionString = TestSupport.ConnectionString;
                Container = TestSupport.Container;
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

            public async Task<string> GetContentAsync(string requestUri)
            {
                using (var httpClient = new HttpClient())
                {
                    return await httpClient.GetStringAsync(requestUri);
                }
            }

            public async Task VerifyCommandResult(CommandResult commandResult, bool direct, bool latest)
            {
                if (direct)
                {
                    await VerifyLineUrl(commandResult, "Direct: ");
                }

                if (latest)
                {
                    await VerifyLineUrl(commandResult, "Latest: ");
                }
            }

            private async Task VerifyLineUrl(CommandResult commandResult, string linePrefix)
            {
                var directLine = commandResult.OutputLines.FirstOrDefault(l => l.StartsWith(linePrefix));
                Assert.NotNull(directLine);

                var directUrl = directLine.Split(new[] { ' ' }, 2)[1];
                var actualContent = await GetContentAsync(directUrl);

                Assert.Equal(Content, actualContent);
            }

            private static string GetToolPath()
            {
                var repositoryRoot = GetRepositoryRoot();

                return Path.Combine(
                    repositoryRoot,
                    "ToStorage.Tool",
                    "bin",
                    "Debug",
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

            public void Dispose()
            {
                TestSupport.DeleteBlobsWithPrefix(Prefix);
            }
        }
    }
}
