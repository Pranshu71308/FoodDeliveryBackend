using System.ComponentModel.DataAnnotations;

namespace AngularDemoAppAPIS.Models
{
    public class VerifyOtpModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Otp { get; set; }
    }
}
