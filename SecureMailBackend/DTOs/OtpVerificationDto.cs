using System.ComponentModel.DataAnnotations;

namespace SecureMailBackend.DTOs
{
    public class OtpVerificationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; }
    }
}
