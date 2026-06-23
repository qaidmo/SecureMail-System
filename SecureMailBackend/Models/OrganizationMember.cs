using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("organization_members")]
    public class OrganizationMember
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("org_id")]
        public int OrgId { get; set; }
        [ForeignKey("OrgId")]
        public Organization Organization { get; set; } 

        [Column("user_id")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } 

        [Required]
        [Column("role")]
        [MaxLength(50)]
        public string Role { get; set; } = "member"; 

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}