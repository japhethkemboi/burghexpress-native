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
[Route("user/{userName}/permissions")]
public class UserPermissionController : ControllerBase
{
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<User> _userManager;

  public UserPermissionController(ApplicationDbContext dbContext, UserManager<User> userManager)
  {
    _dbContext = dbContext;
    _userManager = userManager;
  }


  [HasPermission(ControllerPermissions.UserPermissions.Create)]
  [HttpPost]
  public async Task<ActionResult<UserPermissionResponseDTO>> CreateUserPermission(string userName, UserPermissionCreateDTO data)
  {
    User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    if (user == null)
      return NotFound("User not found.");

    Permission? permission = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Name == data.Permission);
    if (permission == null)
      return CreateProblem.FormValidation("Permission", new[]{ "Enter a valid permission name." }, HttpContext);

    bool alreadyHasPermission = await _dbContext.UserPermissions.AnyAsync(up => up.PermissionId == permission.Id && up.UserId == user.Id);
    if (alreadyHasPermission)
      return CreateProblem.FormValidation("Permission", new[]{ "User already has this permission." }, HttpContext);

    User authUser = await _userManager.GetUserAsync(User);

    UserPermission? userPermission = new UserPermission
    {
      PermissionId = permission.Id,
      Permission = permission,
      UserId = user.Id,
      User = user,
      GrantedById = authUser.Id
    };
    _dbContext.UserPermissions.Add(userPermission);

    await _dbContext.SaveChangesAsync();

    return Created(string.Empty, new
        {
        Message = "Permission granted.",
        UserPermission = new UserPermissionResponseDTO
        {
        Id = userPermission.Id,
        User = new UserResponseDTO 
        {
        UserName = userPermission.User.UserName,
        FirstName = userPermission.User.FirstName,
        LastName = userPermission.User.LastName,
        },
        Permission = new PermissionResponseDTO
        {
        Id = userPermission.Permission.Id,
        Name = userPermission.Permission.Name,
        Description = userPermission.Permission.Description
        }
        }
        });
  }



  [HasPermission(ControllerPermissions.UserPermissions.Patch)]
  [HttpPatch("{userPermissionId}")]
  public async Task<ActionResult<UserPermissionResponseDTO>> PatchUserPermission(string userName, int userPermissionId, UserPermissionCreateDTO patch)
  {
    User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    if(user == null)
      return NotFound("User not found.");

    UserPermission? userPermission = await _dbContext.UserPermissions
      .Include(up => up.User).Include(up => up.Permission).FirstOrDefaultAsync(u => u.Id == userPermissionId);
    if(userPermission == null)
      return NotFound("User permission not found.");

    if(patch.Permission != null)
    {
      Permission? permission = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Name == patch.Permission);
      if(permission == null)
        return CreateProblem.FormValidation("Permission", new[]{ "Enter a valid permission name." }, HttpContext);

      bool alreadyHasPermission = await _dbContext.UserPermissions.AnyAsync(up => up.PermissionId == permission.Id && up.UserId == user.Id);
      if (alreadyHasPermission)
        return CreateProblem.FormValidation("Permission", new[]{ "User already has this permission." }, HttpContext);

      userPermission.PermissionId = permission.Id;
      userPermission.Permission = permission;
    }

    var authUser = _userManager.GetUserAsync(User);
    userPermission.UpdatedById = authUser.Id;

    await _dbContext.SaveChangesAsync();

    return Ok(new 
        {
        Message = "Permission updated.",
        UserPermission = new UserPermissionResponseDTO
        {
        Id = userPermission.Id,
        User = new UserResponseDTO 
        {
        UserName = userPermission.User.UserName,
        FirstName = userPermission.User.FirstName,
        LastName = userPermission.User.LastName,
        },
        Permission = new PermissionResponseDTO
        {
        Id = userPermission.Permission.Id,
        Name = userPermission.Permission.Name,
        Description = userPermission.Permission.Description
        }
        }
        });
  }



  [HasPermission(ControllerPermissions.UserPermissions.View)]
  [HttpGet("{userPermissionId}")]
  public async Task<ActionResult<UserPermissionResponseDTO>> GetUserPermission(string userName, int userPermissionId)
  {
    User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == userName.ToLower());
    if (user == null)
      return NotFound("User not found.");

    UserPermission? userPermission = await _dbContext.UserPermissions
      .Include(up => up.User).Include(up => up.Permission).FirstOrDefaultAsync(u => u.Id == userPermissionId);
    if (userPermission == null)
      return NotFound("User permission not found.");

    return Ok(new UserPermissionResponseDTO
        {
        Id = userPermission.Id,
        User = new UserResponseDTO 
        {
        UserName = userPermission.User.UserName,
        FirstName = userPermission.User.FirstName,
        LastName = userPermission.User.LastName,
        },
        Permission = new PermissionResponseDTO
        {
        Id = userPermission.Permission.Id,
        Name = userPermission.Permission.Name,
        Description = userPermission.Permission.Description
        }
        });
  }



  [HasPermission(ControllerPermissions.UserPermissions.View)]
  [HttpGet]
  public async Task<ActionResult<IEnumerable<UserPermissionResponseDTO>>> GetUserPermissions(string userName)
  {
    User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    if(user == null)
      return NotFound("User not found.");

    List<UserPermissionResponseDTO> userPermissions = await _dbContext.UserPermissions
      .Where(up => up.User.UserName.ToLower() == userName.ToLower())
      .Select(up => new UserPermissionResponseDTO
          {
        Id = up.Id,
        User = new UserResponseDTO 
        {
        UserName = up.User.UserName,
        FirstName = up.User.FirstName,
        LastName = up.User.LastName,
        },
        Permission = new PermissionResponseDTO
        {
        Id = up.Permission.Id,
        Name = up.Permission.Name,
        Description = up.Permission.Description
        }
        }).ToListAsync();
    return Ok(userPermissions);
  }



  [HasPermission(ControllerPermissions.UserPermissions.Delete)]
  [HttpDelete("{userPermissionId}")]
  public async Task<IActionResult> DeleteUserPermission(string userName, int userPermissionId)
  {
    User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    if(user == null)
      return NotFound("User not found.");

    UserPermission? userPermission = await _dbContext.UserPermissions.FirstOrDefaultAsync(up => up.Id == userPermissionId);
    if(userPermission == null)
      return NotFound("User permission not found.");

    _dbContext.UserPermissions.Remove(userPermission);
    await _dbContext.SaveChangesAsync();

    return NoContent();
  }
}
