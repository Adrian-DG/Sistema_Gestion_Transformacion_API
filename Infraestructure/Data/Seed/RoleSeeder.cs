using Application.Security;
using Infraestructure.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data.Seed
{
    /// <summary>
    /// Crea un rol por cada <see cref="AppPermission"/>. Los Ids y ConcurrencyStamp son fijos para que
    /// las migraciones no detecten cambios en cada ejecución.
    /// </summary>
    public static class RoleSeeder
    {
        private static readonly Dictionary<AppPermission, (Guid Id, string Descripcion)> Roles = new()
        {
            [AppPermission.Administrador] = (new("9b0e6c1a-0d6f-4c1e-8f3a-1a0000000001"), "Acceso total al sistema"),
            [AppPermission.GestionVehiculos] = (new("9b0e6c1a-0d6f-4c1e-8f3a-1a0000000002"), "Registro y edición de vehículos"),
            [AppPermission.GestionAsignaciones] = (new("9b0e6c1a-0d6f-4c1e-8f3a-1a0000000003"), "Solicitudes y asignaciones de vehículos"),
            [AppPermission.GestionPolizas] = (new("9b0e6c1a-0d6f-4c1e-8f3a-1a0000000004"), "Pólizas de seguro"),
            [AppPermission.GestionCombustible] = (new("9b0e6c1a-0d6f-4c1e-8f3a-1a0000000005"), "Tickets, compras y despachos de combustible"),
            [AppPermission.GestionOrganizacion] = (new("9b0e6c1a-0d6f-4c1e-8f3a-1a0000000006"), "Dependencias, cargos y personas"),
            [AppPermission.GestionUsuarios] = (new("9b0e6c1a-0d6f-4c1e-8f3a-1a0000000007"), "Administración de usuarios y permisos"),
        };

        public static void Seed(this ModelBuilder builder)
        {
            builder.Entity<AppRole>().HasData(Roles.Select(r => new AppRole
            {
                Id = r.Value.Id,
                Name = r.Key.ToString(),
                NormalizedName = r.Key.ToString().ToUpperInvariant(),
                ConcurrencyStamp = r.Value.Id.ToString(),
                Description = r.Value.Descripcion
            }));
        }
    }
}
