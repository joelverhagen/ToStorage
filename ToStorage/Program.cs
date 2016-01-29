using System;
using System.Threading.Tasks;
using CommandLine;
using Knapcode.ToStorage.AzureBlobStorage;

namespace Knapcode.ToStorage
{
    class Program
    {
        static int Main(string[] args)
        {
            args = new[]
            {
                "-a", "ACCOUNT",
                "-k", "KEY",
                "-c", "CONTAINER",
                "-f", "{0}.json",
                "-t", "application/json"
            };
            return MainAsync(args).Result;
        }

        public static async Task<int> MainAsync(string[] args)
        {
            // parse options
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result.Tag == ParserResultType.NotParsed)
            {
                return 1;
            }

            var options = result.MapResult(o => o, e => null);
            var client = new AzureBlobStorage.Client();
            await client.UploadAsync(options, Console.OpenStandardInput(), Console.Out);
            
            return 0;
        }
    }
}
