using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using BurghExpress.Server.Models;
using BurghExpress.Server.Data;
using BurghExpress.Server.DTO;
using BurghExpress.Server.Permissions;
using BurghExpress.Server.Utils;

namespace BurghExpress.Server.Controllers;


[ApiController]
[Route("roles")]
public class RoleController : ControllerBase
{
  private readonly ApplicationDbContext _dbContext;

  public RoleController(ApplicationDbContext dbContext)
  {
    _dbContext = dbContext;
  }


  [HasPermission(ControllerPermissions.Roles.Create)]
  [HttpPost]
  public async Task<ActionResult<RoleResponseDTO>> CreateRole(RoleCreateDTO data)
  {
    bool existingRole = await _dbContext.Roles.AnyAsync(r => r.Name == data.Name);
    if(existingRole)
      return CreateProblem.FormValidation("Name", new[]{ "Role with this name already exists." }, HttpContext);

    Role newRole = new Role
    {
      Name = data.Name,
      Description = data.Description
    };
    _dbContext.Roles.Add(newRole);
    await _dbContext.SaveChangesAsync();

    return Created(string.Empty, new
        {
        message = "Role created.",
        role = new RoleResponseDTO
        {
        Id = newRole.Id,
        Name = newRole.Name,
        Description = newRole.Description
        }});
  }



  [HasPermission(ControllerPermissions.Roles.Patch)]
  [HttpPatch("{id}")]
  public async Task<ActionResult<RoleResponseDTO>> PatchRole(int id, RolePatchDTO patch)
  {
    Role? role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id);
    if(role == null)
      return NotFound("Role not found.");

    if(patch.Name != null)
    {
      bool roleNameExists = await _dbContext.Roles.AnyAsync(r => r.Name == patch.Name);
      if(roleNameExists)
        return CreateProblem.FormValidation("Name", new[]{ "Role with this name already exists." }, HttpContext);

      role.Name = patch.Name;
    }

    if(patch.Description != null)
      role.Description = patch.Description;

    await _dbContext.SaveChangesAsync();

    return Ok(new PermissionResponseDTO
        {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description
        });
  }



  [HasPermission(ControllerPermissions.Roles.View)]
  [HttpGet("{id}")]
  public async Task<ActionResult<RoleResponseDTO>> GetRole(int id)
  {
    Role? role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id);
    if(role == null)
      return NotFound("Role not found.");

    return Ok(new RoleResponseDTO
        {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description
        });
  }



  [HasPermission(ControllerPermissions.Roles.View)]
  [HttpGet]
  public async Task<ActionResult<IEnumerable<RoleResponseDTO>>> GetRoles()
  {
    var roles = await _dbContext.Roles.Select(r => new RoleResponseDTO
        {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description
        }).ToListAsync();
    return Ok(roles);
  }



  [HasPermission(ControllerPermissions.Roles.Delete)]
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteRole(int id)
  {
    Role? role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id);
    if(role == null)
      return NotFound("Role not found.");

    _dbContext.Roles.Remove(role);
    await _dbContext.SaveChangesAsync();
    return NoContent();
  }
}
