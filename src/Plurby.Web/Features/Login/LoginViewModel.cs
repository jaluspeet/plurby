using System.ComponentModel.DataAnnotations;

namespace Plurby.Web.Features.Login
{
    public class LoginViewModel
    {
        public string ReturnUrl { get; set; }

        [Required(ErrorMessage = "Email richiesta")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password richiesta")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}