using Microsoft.AspNetCore.Http;

namespace BurghExpress.Server.Middlewares;

public class CookieToBearerMiddleware
{
    private readonly RequestDelegate _next;

    public CookieToBearerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(context.Request.Headers["Authorization"]))
        {
            var token = context.Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
              context.Request.Headers["Authorization"] = $"Bearer {token}";
        }

        await _next(context);
    }
}
