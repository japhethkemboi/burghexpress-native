using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace BurghExpress.Server.Utils;

public class CreateProblem
{
  public static ActionResult FormValidation(
      string fieldName,
      string[] errors,
      HttpContext httpContext
      )
  {
    return new BadRequestObjectResult(
        new ValidationProblemDetails(
          new Dictionary<string, string[]> { [fieldName] = errors })
        {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = "One or more validation errors occurred.",
        Status = StatusCodes.Status400BadRequest,
        Extensions =
        {
        ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier
        }
        });
  }
}
