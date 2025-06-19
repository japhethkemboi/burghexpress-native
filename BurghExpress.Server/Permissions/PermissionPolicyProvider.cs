using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BurghExpress.Server.Permissions;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
  private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

  public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
  {
    _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
  }

  public Task<AuthorizationPolicy?> GetDefaultPolicyAsync() =>
    _fallbackPolicyProvider.GetDefaultPolicyAsync();

  public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
    _fallbackPolicyProvider.GetFallbackPolicyAsync();

  public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
  {
    if(policyName.StartsWith("Permission:"))
    {
      var permission = policyName.Substring("Permission:".Length);
      var policy = new AuthorizationPolicyBuilder()
        .AddRequirements(new PermissionRequirement(permission))
        .Build();
      return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    return _fallbackPolicyProvider.GetPolicyAsync(policyName);
  }
}
