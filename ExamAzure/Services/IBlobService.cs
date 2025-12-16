using ExamAzure.Models;

namespace ExamAzure.Services
{
    public interface IBlobService
    {
        Task UploadAsync(string blobName, Stream content, string contentType, IDictionary<string, string> metadata, CancellationToken ct = default);
        Task<bool> ExistsAsync(string blobName, CancellationToken ct = default);
        Task<IEnumerable<BlobItemInfo>> ListLatestAsync(int topN, CancellationToken ct = default);
        Uri GenerateSasUri(string blobName, TimeSpan ttl, bool asAttachment = false);
        Task DeleteAsync(string blobName, CancellationToken ct = default);
    }




}
