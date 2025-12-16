namespace ExamAzure.Models
{
    public class MiniGalleryOptions
    {
        public string ContainerName { get; set; } = "photos";
        public int MaxUploadSizeMb { get; set; } = 10;
        public string FolderPrefix { get; set; } = "vzakusiloExam";
        public string[] AllowedContentTypes { get; set; } = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        public int PreviewSasTtlMinutes { get; set; } = 10;
        public int LinkSasTtlHours { get; set; } = 24;
        public int TopN { get; set; } = 20;
    }

}
