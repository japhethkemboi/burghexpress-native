namespace BurghExpress.Server.Models;

public class Permission
{
  public int Id { get; set; }

  public required string Name { get; set; }
  public string? Description { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.Now;
  public DateTime UpdatedAt { get; set; }
}
