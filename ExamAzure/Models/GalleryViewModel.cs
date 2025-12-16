namespace ExamAzure.Models
{
    public class GalleryViewModel
    {
        public string Name { get; set; } = default!;
        public string? OriginalFileName { get; set; }
        public long SizeBytes { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public string ContentType { get; set; } = default!;
        public string PreviewUri { get; set; } = default!;
        public string SizeHuman => SizeBytes < 1024 ? $"{SizeBytes} B" : (SizeBytes < 1024 * 1024 ? $"{(SizeBytes / 1024.0):F1} KB" : $"{(SizeBytes / (1024.0 * 1024.0)):F2} MB");
    }
}
