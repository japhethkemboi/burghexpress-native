using Microsoft.AspNetCore.Identity;

namespace BurghExpress.Server.Models;

public class User : IdentityUser<int>
{
  public required string FirstName { get; set; }
  public string? LastName { get; set; }
  public string? PhoneNumber { get; set; }
  public bool IsDeleted { get; set; } = false;
  public DateTime CreatedAt { get; set; } = DateTime.Now;
  public DateTime UpdatedAt { get; set; }
}
