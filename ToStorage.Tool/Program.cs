using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            // using (var stdin = new FileStream(@"C:\Users\jver\Dropbox\Programming\ToStorage\artifacts\foo.txt", FileMode.Open))
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
                                var equals = await EqualsAsync(buffer, x.Stream);
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

        private static async Task<bool> EqualsAsync(Stream streamA, Stream streamB)
        {
            var bufferA = new byte[8192];
            var bufferB = new byte[bufferA.Length];
            int readA = 1;
            int readB = 1;
            while (readA > 0 && readB > 0)
            {
                readA = await FillBufferAsync(streamA, bufferA);
                readB = await FillBufferAsync(streamB, bufferB);

                if(readA != readB || !bufferA.Take(readA).SequenceEqual(bufferB.Take(readB)))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<int> FillBufferAsync(Stream stream, byte[] buffer)
        {
            int offset = 0;
            int read = 1;
            while (offset < buffer.Length && read > 0)
            {
                read = await stream.ReadAsync(buffer, offset, buffer.Length - offset);
                offset += read;
            }

            return offset;
        }
    }
}
