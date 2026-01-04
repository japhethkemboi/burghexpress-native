using System.ComponentModel.DataAnnotations;

namespace BurghExpress.Server.DTO;

public class RolePatchDTO
{
  [StringLength(30, ErrorMessage = "Role name must be 30 characters or less.")]
  [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Role name must contain only letters.")]
  public string? Name { get; set; }


  [StringLength(500, ErrorMessage = "Role description must be 500 characters or less.")]
  public string? Description { get; set; }
}
