using Application.Common.DTO;
using Application.Common.Response;
using Domain.Common;
using Domain.Enums;

namespace Application.Contracts.Usuarios;

/// <summary>Administración de usuarios de ASP.NET Identity.</summary>
public interface IUsuarioService
{
    Task<PagedData<UsuarioViewModel>> GetAsync(PaginationFilterQuery<UsuarioViewModel> filter, bool? activo, CancellationToken cancellationToken);
    Task<Result<UsuarioDetalleViewModel>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<RolViewModel>> GetRolesAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> CreateAsync(DatosUsuario datos, string password, IReadOnlyList<string> roles, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(Guid id, DatosUsuario datos, CancellationToken cancellationToken);
    Task<Result> SetRolesAsync(Guid id, IReadOnlyList<string> roles, CancellationToken cancellationToken);
    Task<Result> SetActivoAsync(Guid id, bool activo, CancellationToken cancellationToken);
}

public record DatosUsuario(
    string UserName,
    string Email,
    string? Telefono,
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    Guid? RangoId);

public class UsuarioViewModel
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public required string NombreCompleto { get; set; }
    public required string Identificacion { get; set; }
    public string? Email { get; set; }
    public required string Institucion { get; set; }
    public string? Rango { get; set; }
    public bool Activo { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class UsuarioDetalleViewModel
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public required string Identificacion { get; set; }
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public required string Sexo { get; set; }
    public required string Institucion { get; set; }
    public Guid? RangoId { get; set; }
    public string? Rango { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class RolViewModel
{
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
}
