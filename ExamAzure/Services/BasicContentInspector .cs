namespace ExamAzure.Services
{
    public class BasicContentInspector : IContentInspector
    {
        public Task<string?> InspectContentTypeAsync(Stream stream, CancellationToken ct = default)
        {
            if (!stream.CanSeek)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                return Task.FromResult(Inspect(ms));
            }

            var pos = stream.Position;
            var result = Inspect(stream);
            stream.Position = pos;
            return Task.FromResult(result);
        }

        private string? Inspect(Stream s)
        {
            Span<byte> header = stackalloc byte[12];
            int read = s.Read(header);
            if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return "image/jpeg";
            if (read >= 8 &&
                header[0] == 0x89 && header[1] == (byte)'P' && header[2] == (byte)'N' && header[3] == (byte)'G')
                return "image/png";
            if (read >= 4 && header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F')
                return "image/gif";
            if (read >= 12 &&
                header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
                header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
                return "image/webp";

            return null;
        }
    }

}
