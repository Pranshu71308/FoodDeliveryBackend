namespace AngularDemoAppAPIS.Models
{
    public class UpdateUserDetailsModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phonenumber { get; set; }
        public int? UserAuthorityId { get; set; }
    }
}
