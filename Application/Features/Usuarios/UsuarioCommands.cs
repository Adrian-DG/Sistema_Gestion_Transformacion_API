using Application.Contracts;
using Application.Contracts.Usuarios;
using Application.Security;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Usuarios;

public interface IDatosUsuarioRequest
{
    string UserName { get; }
    string Email { get; }
    string? Telefono { get; }
    string Identificacion { get; }
    string Nombre { get; }
    string Apellido { get; }
    SexoEnum Sexo { get; }
    InstitucionEnum Institucion { get; }
    Guid? RangoId { get; }
}

public record CreateUsuarioCommand(
    string UserName,
    string Email,
    string? Telefono,
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    Guid? RangoId,
    string Password,
    List<string> Roles
) : IRequest<Result<Guid>>, IDatosUsuarioRequest;

public record UpdateUsuarioCommand(
    Guid Id,
    string UserName,
    string Email,
    string? Telefono,
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    Guid? RangoId
) : IRequest<Result>, IDatosUsuarioRequest;

/// <summary>Reemplaza los permisos del usuario por los indicados.</summary>
public record AsignarRolesUsuarioCommand(Guid Id, List<string> Roles) : IRequest<Result>;

public record CambiarEstadoUsuarioCommand(Guid Id, bool Activo) : IRequest<Result>;

public class DatosUsuarioValidator<T> : AbstractValidator<T> where T : IDatosUsuarioRequest
{
    public DatosUsuarioValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(50)
            .Matches(@"^[a-zA-Z0-9._@-]+$").WithMessage("El nombre de usuario solo puede contener letras, números y . _ - @");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.Identificacion).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Sexo).IsInEnum();
        RuleFor(x => x.Institucion).IsInEnum();
    }
}

public class CreateUsuarioCommandValidator : DatosUsuarioValidator<CreateUsuarioCommand>
{
    public CreateUsuarioCommandValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Roles).NotNull();
        RuleForEach(x => x.Roles).NotEmpty();
    }
}

public class UpdateUsuarioCommandValidator : DatosUsuarioValidator<UpdateUsuarioCommand>
{
    public UpdateUsuarioCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class AsignarRolesUsuarioCommandValidator : AbstractValidator<AsignarRolesUsuarioCommand>
{
    public AsignarRolesUsuarioCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Roles).NotNull();
        RuleForEach(x => x.Roles).NotEmpty();
    }
}

public class UsuarioCommandHandler(IUsuarioService usuarios, ICurrentUserService currentUser) :
    IRequestHandler<CreateUsuarioCommand, Result<Guid>>,
    IRequestHandler<UpdateUsuarioCommand, Result>,
    IRequestHandler<AsignarRolesUsuarioCommand, Result>,
    IRequestHandler<CambiarEstadoUsuarioCommand, Result>
{
    private static readonly string RolAdministrador = AppPermission.Administrador.ToString();

    public Task<Result<Guid>> Handle(CreateUsuarioCommand request, CancellationToken cancellationToken) =>
        usuarios.CreateAsync(Datos(request), request.Password, request.Roles, cancellationToken);

    public Task<Result> Handle(UpdateUsuarioCommand request, CancellationToken cancellationToken) =>
        usuarios.UpdateAsync(request.Id, Datos(request), cancellationToken);

    public async Task<Result> Handle(AsignarRolesUsuarioCommand request, CancellationToken cancellationToken)
    {
        var quitaseAdministrador = !request.Roles.Contains(RolAdministrador, StringComparer.OrdinalIgnoreCase);

        if (EsUsuarioActual(request.Id) && quitaseAdministrador)
        {
            var actual = await usuarios.GetByIdAsync(request.Id, cancellationToken);
            if (actual.IsSuccess && actual.Value.Roles.Contains(RolAdministrador))
            {
                return Result.Failure(UsuarioErrors.AccionSobreSiMismo);
            }
        }

        return await usuarios.SetRolesAsync(request.Id, request.Roles, cancellationToken);
    }

    public async Task<Result> Handle(CambiarEstadoUsuarioCommand request, CancellationToken cancellationToken)
    {
        if (EsUsuarioActual(request.Id) && !request.Activo)
        {
            return Result.Failure(UsuarioErrors.AccionSobreSiMismo);
        }

        return await usuarios.SetActivoAsync(request.Id, request.Activo, cancellationToken);
    }

    private bool EsUsuarioActual(Guid id) => currentUser.UserId == id;

    private static DatosUsuario Datos(IDatosUsuarioRequest r) => new(
        r.UserName.Trim(), r.Email.Trim(), r.Telefono, r.Identificacion.Trim(),
        r.Nombre.Trim(), r.Apellido.Trim(), r.Sexo, r.Institucion, r.RangoId);
}
