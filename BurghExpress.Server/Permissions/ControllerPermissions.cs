namespace BurghExpress.Server.Permissions;

public static class ControllerPermissions
{
  public static class UserPermissions
  {
    public const string Create = "UserPermissions.Create";
    public const string Patch = "UserPermissions.Patch";
    public const string View = "UserPermissions.View";
    public const string Delete = "UserPermissions.Delete";
  }

  public static class Roles
  {
    public const string Create = "Roles.Create";
    public const string Patch = "Roles.Patch";
    public const string View = "Roles.View";
    public const string Delete = "Roles.Delete";
  }

  public static class UserRoles
  {
    public const string Create = "UserRoles.Create";
    public const string Patch = "UserRoles.Patch";
    public const string View = "UserRoles.View";
    public const string Delete = "UserRoles.Delete";
  }
}
