using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Column("scan_id")]
        public int? ScanId { get; set; }
        [ForeignKey("ScanId")]
        public Scan? Scan { get; set; } // ربط التنبيه بالفحص الذي سببه

        [Required]
        [Column("title")]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        [Column("body")]
        public string Body { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "SENT"; // SENT, DELIVERED, READ

        [Column("sent_at")]
        public DateTime? SentAt { get; set; } = DateTime.UtcNow;

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }
    }
}