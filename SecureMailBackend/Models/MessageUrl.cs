using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("message_urls")]
    public class MessageUrl
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("message_id")]
        public int MessageId { get; set; }
        [ForeignKey("MessageId")]
        public EmailMessage EmailMessage { get; set; } // ربط مع الرسالة

        [Required]
        [Column("url")]
        public string Url { get; set; }

        [Column("domain")]
        [MaxLength(255)]
        public string? Domain { get; set; }

        [Column("is_shortened")]
        public bool IsShortened { get; set; } = false; // هل الرابط مختصر؟ (bit.ly وغيرها)

        [Column("risk_score")]
        public int RiskScore { get; set; } = 0; // 0-100

        [Column("reputation")]
        [MaxLength(50)]
        public string? Reputation { get; set; } // SAFE, SUSPICIOUS, MALICIOUS

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}