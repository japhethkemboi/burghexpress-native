using Microsoft.AspNetCore.Authorization;

namespace BurghExpress.Server.Permissions;

public class PermissionRequirement : IAuthorizationRequirement
{
  public string PermissionName { get; }

  public PermissionRequirement(string permissionName)
  {
    PermissionName = permissionName;
  }
}
