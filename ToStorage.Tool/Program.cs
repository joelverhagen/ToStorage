using System;
using System.Threading.Tasks;
using CommandLine;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Tool.AzureBlobStorage;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ToStorage.Tool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return MainAsync(args).Result;
        }

        private static async Task<int> MainAsync(string[] args)
        {
            // parse options
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result.Tag == ParserResultType.NotParsed)
            {
                return 1;
            }

            var options = result.MapResult(o => o, e => null);

            // build the implementation models
            var client = new Client(new SystemTime());
            using (var stdin = Console.OpenStandardInput())
            {
                var request = new UploadRequest
                {
                    Container = options.Container,
                    ContentType = options.ContentType,
                    PathFormat = options.PathFormat,
                    UploadLatest = options.UpdateLatest,
                    Stream = stdin,
                    Trace = Console.Out
                };

                // upload
                await client.UploadAsync(options.ConnectionString, request).ConfigureAwait(false);
            }

            return 0;
        }
    }
}
