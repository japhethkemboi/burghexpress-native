namespace BurghExpress.Server.DTO;

public class UserPermissionResponseDTO
{
  public int Id { get; set; }
  public UserResponseDTO User { get; set; }
  public PermissionResponseDTO Permission { get; set; }
}
