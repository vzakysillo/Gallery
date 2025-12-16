namespace ExamAzure.Services
{
    public interface IContentInspector
    {
        Task<string?> InspectContentTypeAsync(Stream stream, CancellationToken ct = default);
    }
}
