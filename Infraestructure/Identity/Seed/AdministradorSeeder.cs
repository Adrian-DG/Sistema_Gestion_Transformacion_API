using Application.Security;
using Domain.Enums;
using Infraestructure.Data;
using Infraestructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Identity.Seed
{
    /// <summary>
    /// Crea el usuario administrador inicial para poder entrar al sistema en una base nueva. A diferencia de
    /// los demás seeders no usa HasData, porque la contraseña debe pasar por <see cref="UserManager{TUser}"/>
    /// para guardarse con hash. Solo actúa si no existe ningún administrador activo, y toma los datos de la
    /// sección <c>AdministradorInicial</c> de la configuración; sin contraseña configurada no hace nada.
    /// </summary>
    public static class AdministradorSeeder
    {
        public const string Seccion = "AdministradorInicial";

        public static async Task SeedAdministradorAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            await using var scope = services.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdministradorSeeder));
            var configuration = provider.GetRequiredService<IConfiguration>();
            var context = provider.GetRequiredService<MainContext>();
            var userManager = provider.GetRequiredService<UserManager<AppUser>>();

            // Las tablas de Identity y el rol Administrador los crean las migraciones.
            if (!await context.Database.CanConnectAsync(cancellationToken)
                || (await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                logger.LogWarning("La base de datos no existe o tiene migraciones pendientes; no se creó el administrador inicial.");
                return;
            }

            var rol = AppPermission.Administrador.ToString();
            var existeAdministrador = await (
                from ur in context.UserRoles
                join r in context.Roles on ur.RoleId equals r.Id
                join u in context.Users on ur.UserId equals u.Id
                where r.Name == rol && u.Activo
                select u.Id).AnyAsync(cancellationToken);

            if (existeAdministrador) return;

            var datos = configuration.GetSection(Seccion);
            var password = datos["Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("No hay administradores activos y {Seccion}:Password no está configurado; no se creó el administrador inicial.", Seccion);
                return;
            }

            var userName = datos["UserName"] is { Length: > 0 } nombreUsuario ? nombreUsuario : "admin";
            if (await userManager.FindByNameAsync(userName) is not null)
            {
                logger.LogWarning("Ya existe el usuario '{UserName}' sin permiso de Administrador activo; no se modificó.", userName);
                return;
            }

            var usuario = new AppUser
            {
                UserName = userName,
                Email = datos["Email"],
                EmailConfirmed = true,
                Identificacion = datos["Identificacion"] is { Length: > 0 } identificacion ? identificacion : "00000000000",
                Nombre = datos["Nombre"] is { Length: > 0 } nombre ? nombre : "Administrador",
                Apellido = datos["Apellido"] is { Length: > 0 } apellido ? apellido : "del Sistema",
                Institucion = InstitucionEnum.MIDE,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var resultado = await userManager.CreateAsync(usuario, password);
            if (resultado.Succeeded)
            {
                resultado = await userManager.AddToRoleAsync(usuario, rol);
            }

            if (!resultado.Succeeded)
            {
                logger.LogError("No se pudo crear el administrador inicial: {Errores}",
                    string.Join(" ", resultado.Errors.Select(e => e.Description)));
                return;
            }

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Se creó el administrador inicial '{UserName}'. Cambie la contraseña tras el primer ingreso.", userName);
        }
    }
}
