using Infraestructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Infraestructure.Identity.Authentication
{
    /// <summary>
    /// Revalida el token en cada petición contra la base de datos: un usuario desactivado pierde el
    /// acceso de inmediato y sus permisos son los actuales, no los que tenía al iniciar sesión.
    /// </summary>
    public static class SesionVigente
    {
        public static async Task ValidarAsync(TokenValidatedContext context)
        {
            var identidad = context.Principal?.Identity as ClaimsIdentity;
            if (identidad is null || !Guid.TryParse(identidad.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id))
            {
                context.Fail("El token no identifica al usuario.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<MainContext>();
            var activo = await db.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => (bool?)u.Activo)
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (activo != true)
            {
                context.Fail("El usuario no existe o está desactivado.");
                return;
            }

            var permisos = await (from ur in db.UserRoles
                                  join r in db.Roles on ur.RoleId equals r.Id
                                  where ur.UserId == id
                                  select r.Name!)
                .ToListAsync(context.HttpContext.RequestAborted);

            foreach (var rol in identidad.FindAll(ClaimTypes.Role).ToList()) identidad.RemoveClaim(rol);
            identidad.AddClaims(permisos.Select(p => new Claim(ClaimTypes.Role, p)));
        }
    }
}
