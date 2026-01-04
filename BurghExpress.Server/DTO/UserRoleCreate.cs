using System.ComponentModel.DataAnnotations;

namespace BurghExpress.Server.DTO;

public class UserRoleCreateDTO
{
  [Required(ErrorMessage = "Role name is required.")]
  public required string Role { get; set; }
}
