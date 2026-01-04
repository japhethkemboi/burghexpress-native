using System.ComponentModel.DataAnnotations;

namespace BurghExpress.Server.DTO;

public class UserPermissionCreateDTO
{
  [Required(ErrorMessage = "Permission name is required.")]
  public required string Permission { get; set; }
}
