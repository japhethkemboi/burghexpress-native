using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BurghExpress.Server.Data;
using BurghExpress.Server.Models;
using BurghExpress.Server.DTO;
using BurghExpress.Server.Permissions;
using BurghExpress.Server.Utils;

namespace BurghExpress.Server.Controllers;

[ApiController]
[Route("user/{userName}/roles")]
public class UserRoleController : ControllerBase
{
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<User> _userManager;

  public UserRoleController(ApplicationDbContext dbContext, UserManager<User> userManager)
  {
    _dbContext = dbContext;
    _userManager = userManager;
  }


  [HasPermission(ControllerPermissions.UserRoles.Create)]
  [HttpPost]
  public async Task<ActionResult<UserRoleResponseDTO>> CreateUserRole(string userName, UserRoleCreateDTO data)
  {
    User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    if (user == null)
      return NotFound("User not found.");

    Role? role = await _dbContext.Roles.FirstOrDefaultAsync(p => p.Name == data.Role);
    if (role == null)
      return CreateProblem.FormValidation("Role", new[]{ "Enter a valid role name." }, HttpContext);

    bool alreadyHasRole = await _dbContext.UserRoles.AnyAsync(up => up.RoleId == role.Id && up.UserId == user.Id);
    if (alreadyHasRole)
      return CreateProblem.FormValidation("Role", new[]{ "User is already in this role." }, HttpContext);

    User authUser = await _userManager.GetUserAsync(User);

    UserRole? userRole = new UserRole
    {
      RoleId = role.Id,
      Role = role,
      UserId = user.Id,
      User = user,
      AddedById = authUser.Id
    };
    _dbContext.UserRoles.Add(userRole);

    await _dbContext.SaveChangesAsync();

    return Created(string.Empty, new
        {
        Message = "User added to role.",
        UserRole = new UserRoleResponseDTO
        {
        Id = userRole.Id,
        User = new UserResponseDTO 
        {
        UserName = userRole.User.UserName,
        FirstName = userRole.User.FirstName,
        LastName = userRole.User.LastName,
        },
        Role = new RoleResponseDTO
        {
        Id = userRole.Role.Id,
        Name = userRole.Role.Name,
        Description = userRole.Role.Description
        }
        }
        });
  }

}
