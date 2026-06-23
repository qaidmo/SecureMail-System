using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("email_messages")]
    public class EmailMessage
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("integration_id")]
        public int IntegrationId { get; set; }
        [ForeignKey("IntegrationId")]
        public EmailIntegration Integration { get; set; }

        [Required]
        [Column("provider_message_id")]
        [MaxLength(255)]
        public string ProviderMessageId { get; set; } // ID الرسالة عند جوجل

        [Column("received_at")]
        public DateTime ReceivedAt { get; set; }

        [Column("subject")]
        [MaxLength(500)]
        public string? Subject { get; set; }

        [Column("from_email")]
        [MaxLength(150)]
        public string FromEmail { get; set; }

        [Column("from_name")]
        [MaxLength(150)]
        public string? FromName { get; set; }

        [Column("snippet")]
        public string? Snippet { get; set; } // مقتطف من نص الرسالة

        [Column("raw_headers")]
        public string? RawHeaders { get; set; } // لتخزين الهيدر للتحليل الأمني

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MessageUrl> Urls { get; set; } 
        public ICollection<MessageAttachment> Attachments { get; set; }
    }
}