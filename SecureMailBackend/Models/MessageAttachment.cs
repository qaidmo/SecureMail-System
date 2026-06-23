using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("message_attachments")]
    public class MessageAttachment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("message_id")]
        public int MessageId { get; set; }
        [ForeignKey("MessageId")]
        public EmailMessage EmailMessage { get; set; }

        [Required]
        [Column("file_name")]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Column("mime_type")]
        [MaxLength(100)]
        public string? MimeType { get; set; } // نوع الملف (pdf, exe, jpg)

        [Column("size_bytes")]
        public long? SizeBytes { get; set; }

        [Column("sha256")]
        [MaxLength(64)]
        public string? Sha256 { get; set; } // بصمة الملف (مهمة جداً للفيروسات)

        [Column("risk_score")]
        public int RiskScore { get; set; } = 0;

        [Column("reputation")]
        [MaxLength(50)]
        public string? Reputation { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}