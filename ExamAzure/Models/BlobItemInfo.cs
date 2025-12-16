namespace ExamAzure.Models
{
    public class BlobItemInfo
    {
        public string Name { get; set; } = default!;
        public string? OriginalFileName { get; set; }
        public long SizeBytes { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public string ContentType { get; set; } = default!;
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}