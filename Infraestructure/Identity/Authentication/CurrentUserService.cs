using Application.Contracts;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infraestructure.Identity.Authentication
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public Guid? UserId
        {
            get
            {
                var value = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public bool TienePermiso(Application.Security.AppPermission permiso) =>
            httpContextAccessor.HttpContext?.User?.IsInRole(permiso.ToString()) == true;
    }
}
