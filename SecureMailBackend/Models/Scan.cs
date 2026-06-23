using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMailBackend.Models
{
    [Table("scans")]
    public class Scan
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
        [Column("scan_type")]
        [MaxLength(50)]
        public string ScanType { get; set; }

        [Column("address_check_id")]
        public int? AddressCheckId { get; set; }
        [ForeignKey("AddressCheckId")]
        public AddressCheck? AddressCheck { get; set; } 
        [Column("message_id")]
        public int? MessageId { get; set; }
        [ForeignKey("MessageId")]
        public EmailMessage? EmailMessage { get; set; } 

        // ---------------------------------

        [Column("risk_score")]
        public int RiskScore { get; set; } // 0-100

        [Column("verdict")]
        [MaxLength(20)]
        public string Verdict { get; set; }

        [Column("reasons_json")]
        public string? ReasonsJson { get; set; } 

        [Column("recommendations_json")]
        public string? RecommendationsJson { get; set; }

        [Column("provider")]
        [MaxLength(100)]
        public string? Provider { get; set; }

        [Column("domain_country")]
        [MaxLength(100)]
        public string? DomainCountry { get; set; }

        [Column("spf_status")]
        public bool? SpfStatus { get; set; }

        [Column("dmarc_status")]
        public bool? DmarcStatus { get; set; }

        [Column("plain_text_body")]
        public string? PlainTextBody { get; set; }

        [Column("phishing_keywords_json")]
        public string? PhishingKeywordsJson { get; set; }

        [Column("malicious_urls_json")]
        public string? MaliciousUrlsJson { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}