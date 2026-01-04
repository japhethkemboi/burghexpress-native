using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using BurghExpress.Server.DTO;
using BurghExpress.Server.Services;
using BurghExpress.Server.Utils;
using BurghExpress.Server.Models;

namespace BurghExpress.Server.Controllers;

[ApiController]
[Route("")]
public class AuthController : ControllerBase
{
  private readonly UserManager<User> _userManager;
  private readonly JwtService _jwtService;

  public AuthController(UserManager<User> userManager, JwtService jwtService)
  {
    _userManager = userManager;
    _jwtService = jwtService;
  }


  [AllowAnonymous]
  [HttpPost("login")]
  public async Task<IActionResult> Login(LoginDTO credentials)
  {
    var user = await _userManager.FindByEmailAsync(credentials.Email);
    if(user == null)
      return CreateProblem.FormValidation("Email", new[]{ "There is no account associated with this email address." }, HttpContext);

    var passwordChecked = await _userManager.CheckPasswordAsync(user, credentials.PassWord);
    if(!passwordChecked)
      return CreateProblem.FormValidation("PassWord", new[]{ "Incorrect password." }, HttpContext);

    var roles = await _userManager.GetRolesAsync(user);

    var token = _jwtService.GenerateToken(user.Id.ToString(), user.UserName, roles.ToArray());

    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = false,
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddMinutes(60)
    };

    Response.Cookies.Append("access_token", token, cookieOptions);

    return Ok(new
        {
        Token = token,
        ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
  }
}
