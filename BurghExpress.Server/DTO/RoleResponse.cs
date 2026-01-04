namespace BurghExpress.Server.DTO;

public class RoleResponseDTO
{
  public int Id { get; set; }
  public required string Name { get; set; }
  public string? Description { get; set; }
}
