using System;
using System.ComponentModel.DataAnnotations;

namespace SecureMailBackend.Models
{
    public class VirusTotalQuota
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime Date { get; set; } // The UTC date this record represents
        
        [Required]
        public int RequestCount { get; set; } // Number of requests made on this date
    }
}
