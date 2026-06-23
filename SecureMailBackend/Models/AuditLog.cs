using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; } 
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("org_id")]
        public int? OrgId { get; set; }
        [ForeignKey("OrgId")]
        public Organization? Organization { get; set; }

        [Required]
        [Column("action")]
        [MaxLength(100)]
        public string Action { get; set; } // LOGIN, SCAN, UPDATE_POLICY

        [Column("entity_type")]
        [MaxLength(50)]
        public string? EntityType { get; set; } // USER, SCAN, POLICY

        [Column("entity_id")]
        public int? EntityId { get; set; } 

        [Column("ip_address")]
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}