namespace Knapcode.ToStorage.Tool.AzureBlobStorage
{
    public class Options
    {
        public string ConnectionString { get; set; }
        public string Container { get; set; }
        public string PathFormat { get; set; }
        public string ContentType { get; set; }
        public bool NoLatest { get; set; }
        public bool NoDirect { get; set; }
        public bool OnlyUnique { get; set; }
    }
}