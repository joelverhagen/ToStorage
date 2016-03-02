using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Knapcode.ToStorage.Core;
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
            if (args.Contains("--debug"))
            {
                args = args.Except(new[] {"--debug"}).ToArray();
                Debugger.Launch();
            }

            // parse options
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result.Tag == ParserResultType.NotParsed)
            {
                return 1;
            }

            var options = result.MapResult(o => o, e => null);

            // build the implementation models
            using (var stdin = Console.OpenStandardInput())
            {
                var client = new Client(new SystemTime());

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
                            IsUniqueAsync = async x =>
                            {
                                var equals = await new AsyncStreamEqualityComparer().EqualsAsync(buffer, x.Stream, CancellationToken.None);
                                buffer.Seek(0, SeekOrigin.Begin);
                                return !equals;
                            },
                            UploadDirect = true,
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
                        UploadLatest = options.UpdateLatest,
                        Stream = stdin,
                        Trace = Console.Out,
                        UploadDirect = options.UpdateDirect
                    };

                    // upload
                    await client.UploadAsync(request);
                }

            }

            return 0;
        }
    }
}
