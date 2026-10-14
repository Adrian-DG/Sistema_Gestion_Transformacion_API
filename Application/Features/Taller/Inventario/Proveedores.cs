using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Application.Features.Misc.ViewModels;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Taller;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Taller.Inventario;

public class ProveedorViewModel
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public string? Rnc { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Busca por nombre, RNC o contacto; <see cref="Activo"/> filtra por estado.</summary>
public record FiltroProveedoresQuery : PaginationFilterQuery<ProveedorViewModel>
{
    public bool? Activo { get; set; }
}

public class FiltroProveedoresQueryValidator : AbstractValidator<FiltroProveedoresQuery>
{
    public FiltroProveedoresQueryValidator() => Include(new PaginationFilterQueryValidator<ProveedorViewModel>());
}

public class GetProveedoresQueryHandler(IUnitOfWork uow)
    : IRequestHandler<FiltroProveedoresQuery, Result<PagedData<ProveedorViewModel>>>
{
    public async Task<Result<PagedData<ProveedorViewModel>>> Handle(FiltroProveedoresQuery request, CancellationToken cancellationToken)
    {
        var query = uow.Query<Proveedor>();

        if (request.Activo is { } activo) query = query.Where(p => p.Status == activo);

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            var texto = request.Param.Trim();
            query = query.Where(p =>
                p.Nombre.Contains(texto) ||
                (p.Rnc != null && p.Rnc.Contains(texto)) ||
                (p.Contacto != null && p.Contacto.Contains(texto)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(p => p.Nombre)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProveedorViewModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Rnc = p.Rnc,
                Telefono = p.Telefono,
                Correo = p.Correo,
                Direccion = p.Direccion,
                Contacto = p.Contacto,
                Activo = p.Status
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<ProveedorViewModel>(rows, request.PageSize, request.PageNumber, total));
    }
}

/// <summary>Proveedores activos para listas de selección.</summary>
public record GetProveedoresCatalogoQuery : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetProveedoresCatalogoQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetProveedoresCatalogoQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetProveedoresCatalogoQuery request, CancellationToken cancellationToken)
    {
        var proveedores = await uow.Query<Proveedor>()
            .Where(p => p.Status)
            .OrderBy(p => p.Nombre)
            .Select(p => new CatalogItemViewModel { Id = p.Id, Nombre = p.Nombre })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CatalogItemViewModel>>(proveedores);
    }
}

public interface IDatosProveedor
{
    string Nombre { get; }
    string? Rnc { get; }
    string? Telefono { get; }
    string? Correo { get; }
    string? Direccion { get; }
    string? Contacto { get; }
}

public record CreateProveedorCommand(
    string Nombre, string? Rnc, string? Telefono, string? Correo, string? Direccion, string? Contacto
) : IRequest<Result<Guid>>, IDatosProveedor;

/// <summary>Actualiza el proveedor; <paramref name="Activo"/> en false lo desactiva (no se elimina).</summary>
public record UpdateProveedorCommand(
    Guid Id, string Nombre, string? Rnc, string? Telefono, string? Correo, string? Direccion, string? Contacto, bool Activo
) : IRequest<Result>, IDatosProveedor;

public class DatosProveedorValidator<T> : AbstractValidator<T> where T : IDatosProveedor
{
    public DatosProveedorValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Rnc).MaximumLength(20);
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.Correo).MaximumLength(150).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Correo));
        RuleFor(x => x.Direccion).MaximumLength(300);
        RuleFor(x => x.Contacto).MaximumLength(150);
    }
}

public class CreateProveedorCommandValidator : DatosProveedorValidator<CreateProveedorCommand>;

public class UpdateProveedorCommandValidator : DatosProveedorValidator<UpdateProveedorCommand>;

internal static class ProveedorReglas
{
    public static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public static async Task<Result> ValidarRncAsync(IUnitOfWork uow, string? rnc, Guid? excluirId, CancellationToken cancellationToken)
    {
        if (rnc is null) return Result.Success();
        var duplicado = await uow.Query<Proveedor>().AnyAsync(p => p.Rnc == rnc && p.Id != excluirId, cancellationToken);
        return duplicado ? Result.Failure(InventarioPiezasErrors.RncDuplicado(rnc)) : Result.Success();
    }

    public static void Aplicar(Proveedor proveedor, IDatosProveedor datos)
    {
        proveedor.Nombre = datos.Nombre.Trim();
        proveedor.Rnc = Opcional(datos.Rnc);
        proveedor.Telefono = Opcional(datos.Telefono);
        proveedor.Correo = Opcional(datos.Correo);
        proveedor.Direccion = Opcional(datos.Direccion);
        proveedor.Contacto = Opcional(datos.Contacto);
    }
}

public class ProveedorCommandsHandler(IUnitOfWork uow) :
    IRequestHandler<CreateProveedorCommand, Result<Guid>>,
    IRequestHandler<UpdateProveedorCommand, Result>
{
    public async Task<Result<Guid>> Handle(CreateProveedorCommand request, CancellationToken cancellationToken)
    {
        var rnc = await ProveedorReglas.ValidarRncAsync(uow, ProveedorReglas.Opcional(request.Rnc), null, cancellationToken);
        if (rnc.IsFailure) return Result.Failure<Guid>(rnc.Error);

        var proveedor = new Proveedor { Nombre = request.Nombre, Status = true };
        ProveedorReglas.Aplicar(proveedor, request);

        await uow.AddAsync(proveedor, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(proveedor.Id);
    }

    public async Task<Result> Handle(UpdateProveedorCommand request, CancellationToken cancellationToken)
    {
        var proveedor = await uow.Tracked<Proveedor>().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (proveedor is null) return Result.Failure(InventarioPiezasErrors.ProveedorNotFound(request.Id));

        var rnc = await ProveedorReglas.ValidarRncAsync(uow, ProveedorReglas.Opcional(request.Rnc), request.Id, cancellationToken);
        if (rnc.IsFailure) return rnc;

        ProveedorReglas.Aplicar(proveedor, request);
        proveedor.Status = request.Activo;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
