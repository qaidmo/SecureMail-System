using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("domain_policies")]
    public class DomainPolicy
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("org_id")]
        public int OrgId { get; set; }
        [ForeignKey("OrgId")]
        public Organization Organization { get; set; }

        [Required]
        [Column("policy_type")]
        [MaxLength(20)]
        public string PolicyType { get; set; } // ALLOW, BLOCK

        [Required]
        [Column("domain")]
        [MaxLength(150)]
        public string Domain { get; set; } // example.com

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}