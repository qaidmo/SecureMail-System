namespace SecureMailBackend.DTOs
{
    /// <summary>
    /// Request DTO for the IMAP connection endpoint.
    /// </summary>
    public class ImapConnectionDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ImapHost { get; set; }   // Optional override (auto-detected if null)
        public int? ImapPort { get; set; }       // Optional override (defaults to 993)
    }
}
