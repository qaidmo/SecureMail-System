using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("scan_rules")]
    public class ScanRule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("org_id")]
        public int? OrgId { get; set; } 
        [ForeignKey("OrgId")]
        public Organization? Organization { get; set; }

        [Required]
        [Column("rule_key")]
        [MaxLength(100)]
        public string RuleKey { get; set; } 

        [Column("weight")]
        public int Weight { get; set; } = 10; 

        [Column("enabled")]
        public bool Enabled { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}