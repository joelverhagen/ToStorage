using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Knapcode.ToStorage.Tool.AzureBlobStorage;
using Microsoft.Extensions.CommandLineUtils;

namespace Knapcode.ToStorage.Tool
{
    public class Program
    {
        private const string DefaultPathFormat = "{0}.txt";
        private const string DefaultContentType = "text/plain";

        public static int Main(string[] args)
        {
            if (args.Contains("--debug", StringComparer.OrdinalIgnoreCase))
            {
                Debugger.Launch();

                args = args
                    .Where(x => !StringComparer.OrdinalIgnoreCase.Equals(x, "--debug"))
                    .ToArray();
            }

            var app = new CommandLineApplication(throwOnUnexpectedArg: false);
            app.Name = "ToStorage";
            app.FullName = "Knapcode.ToStorage.Tool: send standard input (stdin) to Azure Blob storage.";

            var connectionStringOption = app.Option(
                "-s|--connection-string",
                "(required) The connection string for Azure Storage.",
                CommandOptionType.SingleValue);

            var containerOption = app.Option(
                "-c|--container",
                "(required) The container name.",
                CommandOptionType.SingleValue);

            var pathFormatOption = app.Option(
                "-f|--path-format",
                $"The format to use when building the path. Default: '{DefaultPathFormat}'.",
                CommandOptionType.SingleValue);

            var contentTypeOption = app.Option(
                "-t|--content-type",
                $"The content type to set on the blob. Default: '{DefaultContentType}'.",
                CommandOptionType.SingleValue);

            var noLatestOption = app.Option(
                "--no-latest",
                "Don't upload the latest blob.",
                CommandOptionType.NoValue);

            var noDirectOption = app.Option(
                "--no-direct",
                "Don't upload the direct blob.",
                CommandOptionType.NoValue);

            var onlyUniqueOption = app.Option(
                "-u|--only-unique",
                "Only upload if the current upload is different than the lastest blob.",
                CommandOptionType.NoValue);

            var helpOption = app.HelpOption("-h|--help");
            helpOption.Description = "Show help information.";

            try
            {
                app.OnExecute(() =>
                {
                    var options = new Options();

                    var error = false;
                    if (!connectionStringOption.HasValue())
                    {
                        Console.Error.WriteLine("Error: required option -s, --connection-string is missing.");
                        error = true;
                    }

                    if (!containerOption.HasValue())
                    {
                        Console.Error.WriteLine("Error: required option -c, --container is missing.");
                        error = true;
                    }

                    if (error)
                    {
                        return 1;
                    }

                    options.ConnectionString = connectionStringOption.Value();
                    options.Container = containerOption.Value();
                    options.PathFormat = pathFormatOption.Value() ?? DefaultPathFormat;
                    options.ContentType = contentTypeOption.Value() ?? DefaultContentType;
                    options.NoLatest = noLatestOption.HasValue();
                    options.NoDirect = noDirectOption.HasValue();
                    options.OnlyUnique = onlyUniqueOption.HasValue();

                    return ExecuteAsync(options).Result;
                });

                return app.Execute(args);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
        
        private static async Task<int> ExecuteAsync(Options options)
        {
            // build the implementation models
            using (var stdin = Console.OpenStandardInput())
            {
                var client = new Client(new SystemTime(), new PathBuilder());

                if (options.OnlyUnique)
                {
                    var uniqueClient = new UniqueClient(client);

                    // we have to buffer the input for byte-by-byte comparison
                    using (var buffer = new MemoryStream())
                    {
                        await stdin.CopyToAsync(buffer);
                        buffer.Seek(0, SeekOrigin.Begin);

                        var request = new UniqueUploadRequest
                        {
                            ConnectionString = options.ConnectionString,
                            Container = options.Container,
                            ContentType = options.ContentType,
                            PathFormat = options.PathFormat,
                            Stream = buffer,
                            EqualsAsync = null,
                            UploadDirect = !options.NoDirect,
                            Trace = Console.Out
                        };

                        // upload
                        await uniqueClient.UploadAsync(request);
                    }

                }
                else
                {
                    var request = new UploadRequest
                    {
                        ConnectionString = options.ConnectionString,
                        Container = options.Container,
                        ContentType = options.ContentType,
                        PathFormat = options.PathFormat,
                        UploadDirect = !options.NoDirect,
                        UploadLatest = !options.NoLatest,
                        Stream = stdin,
                        Trace = Console.Out
                    };

                    // upload
                    await client.UploadAsync(request);
                }

            }

            return 0;
        }
    }
}
