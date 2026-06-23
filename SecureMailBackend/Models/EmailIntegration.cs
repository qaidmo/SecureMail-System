using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("email_integrations")]
    public class EmailIntegration
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [Column("provider")]
        [MaxLength(50)]
        public string Provider { get; set; } // GMAIL, OUTLOOK

        [Column("provider_account_email")]
        [MaxLength(150)]
        public string? ProviderAccountEmail { get; set; }

        [Required]
        [Column("access_token_enc", TypeName = "longtext")]
        public string AccessTokenEnc { get; set; } 

        [Column("refresh_token_enc", TypeName = "longtext")]
        public string? RefreshTokenEnc { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("status")]
        public string Status { get; set; } = "active"; // active, revoked

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<EmailMessage> Messages { get; set; } 
    }
}