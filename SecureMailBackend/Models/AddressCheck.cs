using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("address_checks")]
    public class AddressCheck
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Column("org_id")]
        public int? OrgId { get; set; } 
        [ForeignKey("OrgId")]
        public Organization? Organization { get; set; }

        [Required]
        [Column("email_address")]
        [MaxLength(150)]
        public string EmailAddress { get; set; } 

        [Column("breach_count")]
        public int BreachCount { get; set; } = 0;

        [Column("risk_score")]
        public int RiskScore { get; set; } = 0;

        [Column("verdict")]
        [MaxLength(20)]
        public string Verdict { get; set; } = "SAFE"; // SAFE, RISK, HIGH

        [Column("details_json")]
        public string? DetailsJson { get; set; } 

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}