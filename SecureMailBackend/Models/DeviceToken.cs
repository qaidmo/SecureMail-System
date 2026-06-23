using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("device_tokens")]
    public class DeviceToken
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [Column("platform")]
        [MaxLength(20)]
        public string Platform { get; set; } // ANDROID, IOS, WEB

        [Required]
        [Column("fcm_token")]
        public string FcmToken { get; set; } // رمز Firebase الطويل

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_seen_at")]
        public DateTime? LastSeenAt { get; set; }
    }
}