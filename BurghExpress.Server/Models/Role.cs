using Microsoft.AspNetCore.Identity;

namespace BurghExpress.Server.Models;

public class Role : IdentityRole<int>
{
  public string? Description { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.Now;
  public DateTime UpdatedAt { get; set; }
}
