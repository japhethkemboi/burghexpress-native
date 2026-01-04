using Microsoft.AspNetCore.Identity;

namespace BurghExpress.Server.Models;

public class UserRole : IdentityUserRole<int>
{
  public User User { get; set; }
  public Role Role { get; set; }

  public DateTime AssignedAt { get; set; } = DateTime.Now;
  public int AssignedById { get; set; }

  public DateTime UpdatedAt { get; set; }
  public int UpdatedById { get; set; }
}
