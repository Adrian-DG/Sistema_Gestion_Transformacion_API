using Application.Security;
using Domain.Enums;
using Infraestructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Data.Seed.Demo
{
    /// <summary>
    /// Un usuario por perfil de trabajo, para probar los permisos. Todos usan la contraseña de
    /// <c>DatosDemo:Password</c>; cada usuario se crea solo si su nombre de usuario no existe.
    /// </summary>
    internal static class UsuarioDemoSeeder
    {
        private record UsuarioDemo(
            string UserName, string Identificacion, string Nombre, string Apellido, SexoEnum Sexo,
            InstitucionEnum Institucion, string? Rango, AppPermission[] Permisos);

        private static readonly UsuarioDemo[] Usuarios =
        [
            new("transporte", "999-1000001-1", "Luis Manuel", "Santos Jiménez", SexoEnum.Masculino, InstitucionEnum.ERD, "Capitán",
                [AppPermission.GestionVehiculos, AppPermission.GestionAsignaciones, AppPermission.GestionPersonas]),
            new("combustible", "999-1000002-2", "Rosa Elena", "Matos Ureña", SexoEnum.Femenino, InstitucionEnum.FARD, "Primer Teniente",
                [AppPermission.GestionCombustible, AppPermission.GestionPersonas]),
            new("seguros", "999-1000003-3", "Héctor", "Valdez Núñez", SexoEnum.Masculino, InstitucionEnum.MIDE, null,
                [AppPermission.GestionPolizas]),
            new("seguridad", "999-1000004-4", "Carmen", "Ortiz Lora", SexoEnum.Femenino, InstitucionEnum.ARD, "Mayor",
                [AppPermission.GestionUsuarios]),
            new("consulta", "999-1000005-5", "José Rafael", "Taveras Polanco", SexoEnum.Masculino, InstitucionEnum.MIDE, null,
                [])
        ];

        public static async Task SeedAsync(IServiceProvider provider, string? password, ILogger logger, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("DatosDemo:Password no está configurado; no se crearon los usuarios de ejemplo.");
                return;
            }

            var userManager = provider.GetRequiredService<UserManager<AppUser>>();
            var context = provider.GetRequiredService<MainContext>();
            var rangos = await context.Rangos.ToDictionaryAsync(r => r.Nombre, r => r.Id, cancellationToken);
            var creados = new List<string>();

            foreach (var datos in Usuarios)
            {
                if (await userManager.FindByNameAsync(datos.UserName) is not null) continue;

                var usuario = new AppUser
                {
                    UserName = datos.UserName,
                    Email = $"{datos.UserName}@transportacion.local",
                    EmailConfirmed = true,
                    Identificacion = datos.Identificacion,
                    Nombre = datos.Nombre,
                    Apellido = datos.Apellido,
                    Sexo = datos.Sexo,
                    Institucion = datos.Institucion,
                    RangoId = datos.Rango is null ? null : rangos[datos.Rango],
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                // Sin transacción, un fallo al asignar permisos dejaría el usuario creado sin ellos.
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                var resultado = await userManager.CreateAsync(usuario, password);
                if (resultado.Succeeded && datos.Permisos.Length > 0)
                {
                    resultado = await userManager.AddToRolesAsync(usuario, datos.Permisos.Select(p => p.ToString()));
                }

                if (!resultado.Succeeded)
                {
                    logger.LogError("No se pudo crear el usuario de ejemplo '{UserName}': {Errores}", datos.UserName,
                        string.Join(" ", resultado.Errors.Select(e => e.Description)));
                    context.ChangeTracker.Clear();
                    continue;
                }

                await transaction.CommitAsync(cancellationToken);
                creados.Add(datos.UserName);
            }

            if (creados.Count > 0)
            {
                logger.LogInformation("Usuarios de ejemplo creados: {Usuarios}.", string.Join(", ", creados));
            }
        }
    }
}
