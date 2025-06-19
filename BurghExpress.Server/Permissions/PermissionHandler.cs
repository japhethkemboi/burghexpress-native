using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BurghExpress.Server.Models;
using BurghExpress.Server.Services;

namespace BurghExpress.Server.Permissions;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
  private readonly IPermissionService _permissionService;

  public PermissionHandler(IPermissionService permissionService)
  {
    _permissionService = permissionService;
  }

  protected override async Task HandleRequirementAsync(
      AuthorizationHandlerContext context,
      PermissionRequirement requirement)
  {
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if(string.IsNullOrEmpty(userId))
      return;

    var hasPermission = await _permissionService.HasPermissionAsync(int.Parse(userId), requirement.PermissionName);
    if(hasPermission)
      context.Succeed(requirement);
  }
}
