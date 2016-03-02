using CommandLine;

namespace Knapcode.ToStorage.Tool.AzureBlobStorage
{
    public class Options
    {
        [Option('s', "connection-string", Required = true, HelpText = "The connection string for Azure Storage.")]
        public string ConnectionString { get; set; }

        [Option('c', "container", Required = true, HelpText = "The container name.")]
        public string Container { get; set; }

        [Option('f', "path-format", Required = false, Default = "{0}.txt", HelpText = "The format to use when building the path.")]
        public string PathFormat { get; set; }

        [Option('t', "content-type", Required = false, Default = "text/plain", HelpText = "The content type to set on the blob.")]
        public string ContentType { get; set; }

        [Option('l', "update-latest", Required = false, Default = true, HelpText = "Whether or not to set the 'latest' blob.")]
        public bool UpdateLatest { get; set; }

        [Option('d', "update-direct", Required = false, Default = true, HelpText = "Whether or not to set the 'direct' blob.")]
        public bool UpdateDirect { get; set; }

        [Option('u', "only-unique", Required = false, Default = false, HelpText = "Whether or not to only update the 'latest' blob if the 'latest' blob will change.")]
        public bool OnlyUnique { get; set; }
    }
}