using Microsoft.AspNetCore.Authorization;

namespace BurghExpress.Server.Permissions;

public class HasPermission : AuthorizeAttribute
{
  public HasPermission(string permission) =>
    Policy = $"Permission:{permission}";
}
