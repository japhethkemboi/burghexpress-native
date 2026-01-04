namespace BurghExpress.Server.DTO;

public class UserRoleResponseDTO
{
  public int Id { get; set; }
  public UserResponseDTO User { get; set; }
  public RoleResponseDTO Role { get; set; }
}
