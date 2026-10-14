using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Usuarios;
using Application.Security;
using Domain.Common;
using Domain.Common.Errors;
using Infraestructure.Data;
using Infraestructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Identity
{
    public class UsuarioService(MainContext context, UserManager<AppUser> userManager) : IUsuarioService
    {
        private static readonly string RolAdministrador = AppPermission.Administrador.ToString();
        private static readonly HashSet<string> RolesValidos = Enum.GetNames<AppPermission>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        public async Task<PagedData<UsuarioViewModel>> GetAsync(PaginationFilterQuery<UsuarioViewModel> filter, bool? activo, CancellationToken cancellationToken)
        {
            var query = context.Users.AsNoTracking();

            if (activo.HasValue)
            {
                query = query.Where(u => u.Activo == activo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Param))
            {
                query = query.Where(u =>
                    u.UserName!.Contains(filter.Param) ||
                    u.Nombre.Contains(filter.Param) ||
                    u.Apellido.Contains(filter.Param) ||
                    u.Identificacion.Contains(filter.Param) ||
                    (u.Email != null && u.Email.Contains(filter.Param)));
            }

            var totalRecords = await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderBy(u => u.Apellido).ThenBy(u => u.Nombre)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => new UsuarioViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    NombreCompleto = u.Apellido + " " + u.Nombre,
                    Identificacion = u.Identificacion,
                    Email = u.Email,
                    Institucion = u.Institucion.ToString(),
                    Rango = u.Rango != null ? u.Rango.Nombre : null,
                    Activo = u.Activo
                })
                .ToListAsync(cancellationToken);

            var roles = await RolesPorUsuarioAsync(rows.Select(r => r.Id).ToList(), cancellationToken);
            foreach (var row in rows)
            {
                row.Roles = roles[row.Id].ToList();
            }

            return new PagedData<UsuarioViewModel>(rows, filter.PageSize, filter.PageNumber, totalRecords);
        }

        public async Task<Result<UsuarioDetalleViewModel>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var usuario = await context.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UsuarioDetalleViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Email = u.Email,
                    Telefono = u.PhoneNumber,
                    Identificacion = u.Identificacion,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Sexo = u.Sexo.ToString(),
                    Institucion = u.Institucion.ToString(),
                    RangoId = u.RangoId,
                    Rango = u.Rango != null ? u.Rango.Nombre : null,
                    Activo = u.Activo,
                    FechaCreacion = u.FechaCreacion
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (usuario is null)
            {
                return Result.Failure<UsuarioDetalleViewModel>(UsuarioErrors.NotFound(id));
            }

            usuario.Roles = (await RolesPorUsuarioAsync([id], cancellationToken))[id].ToList();
            return Result.Success(usuario);
        }

        public async Task<IReadOnlyDictionary<Guid, string>> GetNombresAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        {
            if (ids.Count == 0) return new Dictionary<Guid, string>();

            return await context.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, Nombre = (u.Rango != null ? u.Rango.Nombre + " " : "") + u.Apellido + " " + u.Nombre })
                .ToDictionaryAsync(u => u.Id, u => u.Nombre, cancellationToken);
        }

        public async Task<IReadOnlyList<RolViewModel>> GetRolesAsync(CancellationToken cancellationToken)
        {
            return await context.Roles.AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new RolViewModel { Nombre = r.Name!, Descripcion = r.Description })
                .ToListAsync(cancellationToken);
        }

        public async Task<Result<Guid>> CreateAsync(DatosUsuario datos, string password, IReadOnlyList<string> roles, CancellationToken cancellationToken)
        {
            var validacion = await ValidarDatosAsync(null, datos, cancellationToken);
            if (validacion.IsFailure) return Result.Failure<Guid>(validacion.Error);

            var rolesInvalidos = RolesInvalidos(roles);
            if (rolesInvalidos.Count > 0) return Result.Failure<Guid>(UsuarioErrors.RolesInvalidos(rolesInvalidos));

            var usuario = new AppUser
            {
                Identificacion = datos.Identificacion,
                Nombre = datos.Nombre,
                Apellido = datos.Apellido,
                Activo = true,
                FechaCreacion = DateTime.Now
            };
            Aplicar(usuario, datos);

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var creado = await userManager.CreateAsync(usuario, password);
            if (!creado.Succeeded) return Result.Failure<Guid>(Descartar(creado));

            if (roles.Count > 0)
            {
                var asignados = await userManager.AddToRolesAsync(usuario, roles.Select(Normalizar));
                if (!asignados.Succeeded) return Result.Failure<Guid>(Descartar(asignados));
            }

            await transaction.CommitAsync(cancellationToken);
            return Result.Success(usuario.Id);
        }

        public async Task<Result> UpdateAsync(Guid id, DatosUsuario datos, CancellationToken cancellationToken)
        {
            var usuario = await userManager.FindByIdAsync(id.ToString());
            if (usuario is null) return Result.Failure(UsuarioErrors.NotFound(id));

            var validacion = await ValidarDatosAsync(id, datos, cancellationToken);
            if (validacion.IsFailure) return validacion;

            Aplicar(usuario, datos);

            var actualizado = await userManager.UpdateAsync(usuario);
            return actualizado.Succeeded ? Result.Success() : Result.Failure(Descartar(actualizado));
        }

        public async Task<Result> SetRolesAsync(Guid id, IReadOnlyList<string> roles, CancellationToken cancellationToken)
        {
            var rolesInvalidos = RolesInvalidos(roles);
            if (rolesInvalidos.Count > 0) return Result.Failure(UsuarioErrors.RolesInvalidos(rolesInvalidos));

            var usuario = await userManager.FindByIdAsync(id.ToString());
            if (usuario is null) return Result.Failure(UsuarioErrors.NotFound(id));

            var nuevos = roles.Select(Normalizar).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actuales = await userManager.GetRolesAsync(usuario);

            var quitaAdministrador = actuales.Contains(RolAdministrador) && !nuevos.Contains(RolAdministrador);
            if (quitaAdministrador && usuario.Activo && !await ExisteOtroAdministradorActivoAsync(id, cancellationToken))
            {
                return Result.Failure(UsuarioErrors.UltimoAdministrador);
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var quitar = actuales.Where(r => !nuevos.Contains(r)).ToList();
            if (quitar.Count > 0)
            {
                var quitados = await userManager.RemoveFromRolesAsync(usuario, quitar);
                if (!quitados.Succeeded) return Result.Failure(Descartar(quitados));
            }

            var agregar = nuevos.Where(r => !actuales.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
            if (agregar.Count > 0)
            {
                var agregados = await userManager.AddToRolesAsync(usuario, agregar);
                if (!agregados.Succeeded) return Result.Failure(Descartar(agregados));
            }

            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> SetActivoAsync(Guid id, bool activo, CancellationToken cancellationToken)
        {
            var usuario = await userManager.FindByIdAsync(id.ToString());
            if (usuario is null) return Result.Failure(UsuarioErrors.NotFound(id));
            if (usuario.Activo == activo) return Result.Success();

            if (!activo
                && await userManager.IsInRoleAsync(usuario, RolAdministrador)
                && !await ExisteOtroAdministradorActivoAsync(id, cancellationToken))
            {
                return Result.Failure(UsuarioErrors.UltimoAdministrador);
            }

            usuario.Activo = activo;

            var actualizado = await userManager.UpdateAsync(usuario);
            if (!actualizado.Succeeded) return Result.Failure(Descartar(actualizado));

            if (!activo)
            {
                // Invalida cualquier proceso que dependa del sello de seguridad (ej. restablecer contraseña).
                await userManager.UpdateSecurityStampAsync(usuario);
            }

            return Result.Success();
        }

        private async Task<Result> ValidarDatosAsync(Guid? id, DatosUsuario datos, CancellationToken cancellationToken)
        {
            var identificacionDuplicada = await context.Users.AnyAsync(
                u => u.Identificacion == datos.Identificacion && u.Id != id, cancellationToken);

            if (identificacionDuplicada)
            {
                return Result.Failure(UsuarioErrors.Identity("IdentificacionDuplicada",
                    [$"Ya existe un usuario con la identificación '{datos.Identificacion}'."]));
            }

            if (datos.RangoId.HasValue && !await context.Rangos.AnyAsync(r => r.Id == datos.RangoId, cancellationToken))
            {
                return Result.Failure(UsuarioErrors.Identity("RangoInvalido", ["El rango indicado no existe."]));
            }

            return Result.Success();
        }

        private static void Aplicar(AppUser usuario, DatosUsuario datos)
        {
            usuario.UserName = datos.UserName;
            usuario.Email = datos.Email;
            usuario.PhoneNumber = datos.Telefono;
            usuario.Identificacion = datos.Identificacion;
            usuario.Nombre = datos.Nombre;
            usuario.Apellido = datos.Apellido;
            usuario.Sexo = datos.Sexo;
            usuario.Institucion = datos.Institucion;
            usuario.RangoId = datos.RangoId;
        }

        private Task<bool> ExisteOtroAdministradorActivoAsync(Guid excluirId, CancellationToken cancellationToken) =>
            (from ur in context.UserRoles
             join r in context.Roles on ur.RoleId equals r.Id
             join u in context.Users on ur.UserId equals u.Id
             where r.Name == RolAdministrador && u.Activo && u.Id != excluirId
             select u.Id).AnyAsync(cancellationToken);

        private async Task<ILookup<Guid, string>> RolesPorUsuarioAsync(List<Guid> userIds, CancellationToken cancellationToken)
        {
            var roles = await (from ur in context.UserRoles
                               join r in context.Roles on ur.RoleId equals r.Id
                               where userIds.Contains(ur.UserId)
                               orderby r.Name
                               select new { ur.UserId, r.Name }).ToListAsync(cancellationToken);

            return roles.ToLookup(x => x.UserId, x => x.Name!);
        }

        private static List<string> RolesInvalidos(IEnumerable<string> roles) =>
            roles.Where(r => !RolesValidos.Contains(r)).ToList();

        /// <summary>Devuelve el nombre del rol con la capitalización del enum.</summary>
        private static string Normalizar(string rol) =>
            Enum.GetNames<AppPermission>().First(n => n.Equals(rol, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Descarta los cambios pendientes del contexto para que una operación fallida no quede
        /// a medio aplicar (ej. roles agregados) ni deje la entidad con valores rechazados.
        /// </summary>
        private Error Descartar(IdentityResult result)
        {
            context.ChangeTracker.Clear();
            return IdentityError(result);
        }

        private static Error IdentityError(IdentityResult result) =>
            UsuarioErrors.Identity(result.Errors.FirstOrDefault()?.Code ?? "Error", result.Errors.Select(e => e.Description));
    }
}
