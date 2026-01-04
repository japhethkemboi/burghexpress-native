using System.ComponentModel.DataAnnotations;

namespace BurghExpress.Server.DTO;

public class LoginDTO
{
  [Required(ErrorMessage = "Email address is required.")]
  [RegularExpression(@"^[\w\.-]+@[\w\.-]+\.\w{2,}$", ErrorMessage = "Enter a valid email address.")]
  public required string Email { get; set; }


  [Required(ErrorMessage = "PassWord is required.")]
  public required string PassWord { get; set; }
}
