using System.ComponentModel.DataAnnotations;

namespace AngularDemoAppAPIS.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        public string ?Email { get; set; }
    }
}
