namespace SecureMailBackend.DTOs
{
    /// <summary>
    /// Carries both filename and raw binary data for an email attachment,
    /// enabling deep analysis such as YARA rule scanning.
    /// </summary>
    public class AttachmentPayload
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public long SizeBytes { get; set; }
    }
}
