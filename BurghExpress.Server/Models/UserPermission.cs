namespace BurghExpress.Server.Models;

public class UserPermission
{
  public int Id { get; set; }

  public int UserId { get; set; }
  public User User { get; set; }

  public int PermissionId { get; set; }
  public Permission Permission { get; set; }

  public DateTime GrantedAt { get; set; } = DateTime.Now;
  public int GrantedById { get; set; }

  public DateTime UpdatedAt { get; set; }
  public int UpdatedById { get; set; }
}
