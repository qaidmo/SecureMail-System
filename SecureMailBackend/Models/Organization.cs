using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("organizations")]
    public class Organization
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("domain")]
        [MaxLength(100)]
        public string? Domain { get; set; } 

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- العلاقات ---
        public ICollection<OrganizationMember> Members { get; set; } 
        public ICollection<DomainPolicy> Policies { get; set; } 
    }
}