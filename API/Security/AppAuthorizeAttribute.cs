using Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace API.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AppAuthorizeAttribute : AuthorizeAttribute
{
    public AppAuthorizeAttribute(AppPolicy policy)
    {
        Policy = policy.ToString();
    }

    public AppAuthorizeAttribute(params AppPermission[] permissions)
    {
        Roles = string.Join(",", permissions.Select(x => x.ToString()));
    }

    public AppAuthorizeAttribute(AppPolicy policy, params AppPermission[] permissions)
    {
        Policy = policy.ToString();

        if (permissions.Length > 0)
        {
            Roles = string.Join(",", permissions.Select(x => x.ToString()));
        }
    }
}
