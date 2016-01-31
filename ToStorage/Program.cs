using System;
using System.Threading.Tasks;
using CommandLine;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ToStorage
{
    class Program
    {
        static int Main(string[] args)
        {
            return MainAsync(args).Result;
        }

        public static async Task<int> MainAsync(string[] args)
        {
            // parse options
            var result = Parser.Default.ParseArguments<AzureBlobStorage.Options>(args);
            if (result.Tag == ParserResultType.NotParsed)
            {
                return 1;
            }

            var options = result.MapResult(o => o, e => null);
            if ((options.Account == null || options.Key == null) && options.ConnectionString == null)
            {
                Console.WriteLine("Either a connection string must be specified, or the account and key.");
                return 1;
            }

            // build the implementation models
            var client = new Client();
            using (var stdin = Console.OpenStandardInput())
            {
                var request = new UploadRequest
                {
                    Container = options.Container,
                    ContentType = options.ContentType,
                    PathFormat = options.PathFormat,
                    UpdateLatest = options.UpdateLatest,
                    Stream = stdin,
                    Trace = Console.Out
                };

                // upload
                if (options.ConnectionString != null)
                {
                    await client.UploadAsync(options.ConnectionString, request).ConfigureAwait(false);
                }
                else
                {
                    await client.UploadAsync(options.Account, options.Key, request).ConfigureAwait(false);
                }
            }

            return 0;
        }
    }
}
