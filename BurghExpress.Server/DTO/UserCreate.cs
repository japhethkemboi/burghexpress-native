using System.ComponentModel.DataAnnotations;

namespace BurghExpress.Server.DTO;

public class UserCreateDTO
{
  [StringLength(30, ErrorMessage = "Username must be 30 characters or less.")]
  [RegularExpression(@"^[a-zA-Z0-9_]", ErrorMessage = "Username must contain only letters, numbers and underscores.")]
  public string? UserName { get; set; }


  [Required(ErrorMessage = "FirstName is required.")]
  [StringLength(30, ErrorMessage = "FirstName must be 30 characters or less.")]
  [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "FirstName contains invalid characters.")]
  public required string FirstName { get; set; }


  [StringLength(30, ErrorMessage = "LastName must be 30 characters or less.")]
  [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "LastName contains invalid characters.")]
  public string? LastName { get; set; }


  [MinLength(9, ErrorMessage = "PhoneNumber must be atleast 9 characters long.")]
  [MaxLength(12, ErrorMessage = "PhoneNumber must not be more than 10 characters long.")]
  [RegularExpression(@"^[0-9]+$", ErrorMessage = "PhoneNumber must contain digits only.")]
  public string? PhoneNumber { get; set; }


  [Required(ErrorMessage = "Email address is required.")]
  [RegularExpression(@"^[\w\.-]+@[\w\.-]+\.\w{2,}$", ErrorMessage = "Enter a valid email address.")]
  public required string Email { get; set; }


  [Required(ErrorMessage = "PassWord is required.")]
  [MinLength(8, ErrorMessage = "PassWord must be atleast 8 characters long.")]
  [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "PassWord must have atleast a digit, a lowercase and an uppercase letter..")]
  public required string PassWord { get; set; }
}
