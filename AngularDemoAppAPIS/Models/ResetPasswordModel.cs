using System.ComponentModel.DataAnnotations;

namespace AngularDemoAppAPIS.Models
{
    public class ResetPasswordModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string NewPassword { get; set; }

    }
}
