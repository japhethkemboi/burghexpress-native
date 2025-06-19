using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BurghExpress.Server.Models;
using BurghExpress.Server.Data;


namespace BurghExpress.Server.Services;


public interface IPermissionService
{
  Task<bool> HasPermissionAsync(int userId, string permissionName);
}


public class PermissionService : IPermissionService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<User> _userManager;

  public PermissionService(ApplicationDbContext dbContext, UserManager<User> userManager)
  {
    _dbContext = dbContext;
    _userManager = userManager;
  }

  public async Task<bool> HasPermissionAsync(int userId, string permissionName)
  {    
    var userHasPermission = await _dbContext.UserPermissions.AnyAsync(up => up.UserId == userId && up.Permission.Name == permissionName);
    if(userHasPermission) return true;

    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null) return false;

    var userRoles = await _userManager.GetRolesAsync(user);
    if(userRoles.Any())
    {
      var roleHasPermission = await _dbContext.RolePermissions.AnyAsync(rp => userRoles.Contains(rp.Role.Name) && rp.Permission.Name == permissionName);
      if(roleHasPermission) return true;
    }

    return false;
  }
}
