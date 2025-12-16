using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using ExamAzure.Models;
using Microsoft.Extensions.Options;

namespace ExamAzure.Services
{
    public class AzureBlobService : IBlobService
    {
        private readonly BlobContainerClient _container;
        private readonly MiniGalleryOptions _options;
        private readonly ILogger<AzureBlobService> _log;

        public AzureBlobService(BlobServiceClient blobServiceClient, IOptions<MiniGalleryOptions> options, ILogger<AzureBlobService> log)
        {
            _options = options.Value;
            _log = log;
            _container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            _container.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.None);
        }

        private string FullBlobPath(string blobName) => string.IsNullOrEmpty(_options.FolderPrefix) ? blobName : $"{_options.FolderPrefix}/{blobName}";

        public async Task UploadAsync(string blobName, Stream content, string contentType, IDictionary<string, string> metadata, CancellationToken ct = default)
        {
            var blobClient = _container.GetBlobClient(FullBlobPath(blobName));
            var headers = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType };
            content.Position = 0;
            await blobClient.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobUploadOptions
            {
                HttpHeaders = headers,
                Metadata = metadata
            }, ct);
        }

        public async Task<bool> ExistsAsync(string blobName, CancellationToken ct = default)
            => await _container.GetBlobClient(FullBlobPath(blobName)).ExistsAsync(ct);

        public async Task<IEnumerable<BlobItemInfo>> ListLatestAsync(int topN, CancellationToken ct = default)
        {
            var result = new List<BlobItemInfo>();
            await foreach (var item in _container.GetBlobsAsync(prefix: _options.FolderPrefix, traits: Azure.Storage.Blobs.Models.BlobTraits.Metadata, cancellationToken: ct))
            {
                result.Add(new BlobItemInfo
                {
                    Name = item.Name.Substring(_options.FolderPrefix.Length + 1),
                    ContentType = item.Properties.ContentType ?? "application/octet-stream",
                    LastModified = item.Properties.LastModified ?? DateTimeOffset.MinValue,
                    SizeBytes = item.Properties.ContentLength ?? 0,
                    Metadata = item.Metadata
                });
            }
            return result.OrderByDescending(x => x.LastModified).Take(topN);
        }

        public Uri GenerateSasUri(string blobName, TimeSpan ttl, bool asAttachment = false)
        {
            var blobClient = _container.GetBlobClient(FullBlobPath(blobName));
            if (!_container.CanGenerateSasUri)
                throw new InvalidOperationException("SAS generation not available with current credential");

            var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
            {
                BlobContainerName = _container.Name,
                BlobName = FullBlobPath(blobName),
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(ttl)
            };
            sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);
            sasBuilder.ContentDisposition = asAttachment ? "attachment" : "inline";

            return blobClient.GenerateSasUri(sasBuilder);
        }

        public async Task DeleteAsync(string blobName, CancellationToken ct = default)
            => await _container.GetBlobClient(FullBlobPath(blobName)).DeleteIfExistsAsync(cancellationToken: ct);
    }

}