using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BurghExpress.Server.Data;
using BurghExpress.Server.Models;

namespace BurghExpress.Tests.IntegrationTests.Helpers;

public static class AuthHelper
{
  public static async Task<HttpClient> GetAuthenticatedClientAsync(HttpClient client, UserManager<User> userManager, ApplicationDbContext dbContext, string username = "testusername", string email = "testemail@gmail.com", string password = "T3stP@ssw0rd", string[]? roles = null, string[]? permissions = null)
  {
    User? user = await userManager.FindByNameAsync(username);
    if (user == null)
    {
      Status status = new Status { Name = "SomeName" };
      dbContext.Statuses.Add(status);
      await dbContext.SaveChangesAsync();

      user = new User
      {
        UserName = username,
        Email = email,
        FirstName = "TestName",
        StatusId = status.Id,
        Status = status
      };
      await userManager.CreateAsync(user, password);
    }


    if (roles != null)
    {
      foreach(var role in roles)
      {
        bool roleExists = await dbContext.Roles.AnyAsync(r => r.NormalizedName == role.ToUpper());
        if(!roleExists)
          dbContext.Roles.Add(new Role { Name = role, NormalizedName = role.ToUpper() });
      }
      await dbContext.SaveChangesAsync();

      var existingRoles = await userManager.GetRolesAsync(user);
      await userManager.RemoveFromRolesAsync(user, existingRoles);
      await userManager.AddToRolesAsync(user, roles);
    }


    if(permissions != null)
    {
      foreach(var permissionName in permissions)
      {
        Permission? permission = await dbContext.Permissions.FirstOrDefaultAsync(p => p.Name == permissionName);
        if(permission == null)
        {
          permission = new Permission { Name = permissionName };
          dbContext.Permissions.Add(permission);
        }

        UserPermission userPermission = new UserPermission
        {
          UserId = user.Id,
          User = user,
          PermissionId = permission.Id,
          Permission = permission
        };
        dbContext.UserPermissions.Add(userPermission);
      }
      await dbContext.SaveChangesAsync();
    }

    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/login", new { Email = email, PassWord = password });

    loginResponse.EnsureSuccessStatusCode();

    return client;
  }}
