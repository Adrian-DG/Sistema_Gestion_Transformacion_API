using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Taller;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Taller.Inventario;

public class PiezaViewModel
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public string? CodigoBarras { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public required string UnidadMedida { get; set; }
    public decimal ExistenciaMinima { get; set; }
    public decimal Existencia { get; set; }
    /// <summary>Existencia por debajo de la mínima: hay que reponer.</summary>
    public bool BajoMinimo { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Busca por código, código de barras, nombre o descripción; <see cref="SoloBajoMinimo"/> deja las que hay que reponer.</summary>
public record FiltroPiezasQuery : PaginationFilterQuery<PiezaViewModel>
{
    public bool? Activo { get; set; }
    public bool SoloBajoMinimo { get; set; }
}

public class FiltroPiezasQueryValidator : AbstractValidator<FiltroPiezasQuery>
{
    public FiltroPiezasQueryValidator() => Include(new PaginationFilterQueryValidator<PiezaViewModel>());
}

public class GetPiezasQueryHandler(IUnitOfWork uow) : IRequestHandler<FiltroPiezasQuery, Result<PagedData<PiezaViewModel>>>
{
    public async Task<Result<PagedData<PiezaViewModel>>> Handle(FiltroPiezasQuery request, CancellationToken cancellationToken)
    {
        var movimientos = uow.Query<MovimientoPieza>().Where(m => m.Status);
        var piezas = uow.Query<Pieza>();

        if (request.Activo is { } activo) piezas = piezas.Where(p => p.Status == activo);

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            var texto = request.Param.Trim();
            piezas = piezas.Where(p =>
                p.Codigo.Contains(texto) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(texto)) ||
                p.Nombre.Contains(texto) ||
                (p.Descripcion != null && p.Descripcion.Contains(texto)));
        }

        var query = piezas.Select(p => new PiezaViewModel
        {
            Id = p.Id,
            Codigo = p.Codigo,
            CodigoBarras = p.CodigoBarras,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            UnidadMedida = p.UnidadMedida,
            ExistenciaMinima = p.ExistenciaMinima,
            Existencia = movimientos
                .Where(m => m.PiezaId == p.Id)
                .Sum(m => m.Concepto == ConceptoMovimientoPieza.Consumo ? -m.Cantidad : m.Cantidad),
            Activo = p.Status
        });

        if (request.SoloBajoMinimo) query = query.Where(p => p.Existencia < p.ExistenciaMinima);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(p => p.Nombre)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        rows.ForEach(p => p.BajoMinimo = p.Existencia < p.ExistenciaMinima);
        return Result.Success(new PagedData<PiezaViewModel>(rows, request.PageSize, request.PageNumber, total));
    }
}

public class PiezaCatalogoViewModel
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    /// <summary>Para ubicar la pieza al escanearla.</summary>
    public string? CodigoBarras { get; set; }
    public required string Nombre { get; set; }
    public required string UnidadMedida { get; set; }
    public decimal Existencia { get; set; }
}

/// <summary>Piezas activas con su existencia, para elegirlas en una orden de taller o una entrada.</summary>
public record GetPiezasCatalogoQuery : IRequest<Result<IReadOnlyList<PiezaCatalogoViewModel>>>;

public class GetPiezasCatalogoQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetPiezasCatalogoQuery, Result<IReadOnlyList<PiezaCatalogoViewModel>>>
{
    public async Task<Result<IReadOnlyList<PiezaCatalogoViewModel>>> Handle(GetPiezasCatalogoQuery request, CancellationToken cancellationToken)
    {
        var movimientos = uow.Query<MovimientoPieza>().Where(m => m.Status);
        var piezas = await uow.Query<Pieza>()
            .Where(p => p.Status)
            .OrderBy(p => p.Nombre)
            .Select(p => new PiezaCatalogoViewModel
            {
                Id = p.Id,
                Codigo = p.Codigo,
                CodigoBarras = p.CodigoBarras,
                Nombre = p.Nombre,
                UnidadMedida = p.UnidadMedida,
                Existencia = movimientos
                    .Where(m => m.PiezaId == p.Id)
                    .Sum(m => m.Concepto == ConceptoMovimientoPieza.Consumo ? -m.Cantidad : m.Cantidad)
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PiezaCatalogoViewModel>>(piezas);
    }
}

public interface IDatosPieza
{
    string Codigo { get; }
    string Nombre { get; }
    string? Descripcion { get; }
    string UnidadMedida { get; }
    decimal ExistenciaMinima { get; }
    string? CodigoBarras { get; }
}

/// <summary>Registra una pieza sin existencia; entra al inventario con una entrada o una compra directa.</summary>
public record CreatePiezaCommand(string Codigo, string Nombre, string? Descripcion, string UnidadMedida, decimal ExistenciaMinima, string? CodigoBarras = null)
    : IRequest<Result<Guid>>, IDatosPieza;

/// <summary>Actualiza la pieza; <paramref name="Activo"/> en false la desactiva (conserva su kardex).</summary>
public record UpdatePiezaCommand(Guid Id, string Codigo, string Nombre, string? Descripcion, string UnidadMedida, decimal ExistenciaMinima, bool Activo, string? CodigoBarras = null)
    : IRequest<Result>, IDatosPieza;

public class DatosPiezaValidator<T> : AbstractValidator<T> where T : IDatosPieza
{
    public DatosPiezaValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descripcion).MaximumLength(500);
        RuleFor(x => x.UnidadMedida).NotEmpty().MaximumLength(30);
        RuleFor(x => x.ExistenciaMinima).GreaterThanOrEqualTo(0).PrecisionScale(12, 2, true);
        // Lo que envía el lector: caracteres imprimibles sin espacios (EAN/UPC, Code 128, Code 39…).
        // Se valida ya recortado: algunos lectores agregan un espacio o un salto de línea al final.
        RuleFor(x => x.CodigoBarras)
            .Must(c => c!.Trim().Length <= 64).WithMessage("El código de barras debe tener un máximo de 64 caracteres.")
            .Must(c => System.Text.RegularExpressions.Regex.IsMatch(c!.Trim(), @"^[\x21-\x7E]+$"))
            .WithMessage("El código de barras solo admite letras, números y símbolos, sin espacios.")
            .When(x => !string.IsNullOrWhiteSpace(x.CodigoBarras));
    }
}

public class CreatePiezaCommandValidator : DatosPiezaValidator<CreatePiezaCommand>;

public class UpdatePiezaCommandValidator : DatosPiezaValidator<UpdatePiezaCommand>;

public class PiezaCommandsHandler(IUnitOfWork uow) :
    IRequestHandler<CreatePiezaCommand, Result<Guid>>,
    IRequestHandler<UpdatePiezaCommand, Result>
{
    public async Task<Result<Guid>> Handle(CreatePiezaCommand request, CancellationToken cancellationToken)
    {
        var codigo = request.Codigo.Trim().ToUpperInvariant();
        if (await uow.Query<Pieza>().AnyAsync(p => p.Codigo == codigo, cancellationToken))
        {
            return Result.Failure<Guid>(InventarioPiezasErrors.CodigoDuplicado(codigo));
        }

        var barras = await ValidarCodigoBarrasAsync(request.CodigoBarras, null, cancellationToken);
        if (barras.IsFailure) return Result.Failure<Guid>(barras.Error);

        var pieza = new Pieza { Codigo = codigo, Nombre = request.Nombre, UnidadMedida = request.UnidadMedida, Status = true };
        Aplicar(pieza, request, codigo);

        await uow.AddAsync(pieza, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(pieza.Id);
    }

    public async Task<Result> Handle(UpdatePiezaCommand request, CancellationToken cancellationToken)
    {
        var pieza = await uow.Tracked<Pieza>().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (pieza is null) return Result.Failure(InventarioPiezasErrors.PiezaNotFound(request.Id));

        var codigo = request.Codigo.Trim().ToUpperInvariant();
        if (await uow.Query<Pieza>().AnyAsync(p => p.Codigo == codigo && p.Id != request.Id, cancellationToken))
        {
            return Result.Failure(InventarioPiezasErrors.CodigoDuplicado(codigo));
        }

        var barras = await ValidarCodigoBarrasAsync(request.CodigoBarras, request.Id, cancellationToken);
        if (barras.IsFailure) return barras;

        Aplicar(pieza, request, codigo);
        pieza.Status = request.Activo;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>El código de barras identifica la pieza al escanearla: no puede repetirse, ni en piezas desactivadas.</summary>
    private async Task<Result> ValidarCodigoBarrasAsync(string? codigoBarras, Guid? excluirId, CancellationToken cancellationToken)
    {
        var barras = NormalizarCodigoBarras(codigoBarras);
        if (barras is null) return Result.Success();

        var otra = await uow.Query<Pieza>()
            .Where(p => p.CodigoBarras == barras && p.Id != excluirId)
            .Select(p => p.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

        return otra is null ? Result.Success() : Result.Failure(InventarioPiezasErrors.CodigoBarrasDuplicado(barras, otra));
    }

    private static string? NormalizarCodigoBarras(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static void Aplicar(Pieza pieza, IDatosPieza datos, string codigo)
    {
        pieza.Codigo = codigo;
        pieza.CodigoBarras = NormalizarCodigoBarras(datos.CodigoBarras);
        pieza.Nombre = datos.Nombre.Trim();
        pieza.Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : datos.Descripcion.Trim();
        pieza.UnidadMedida = datos.UnidadMedida.Trim();
        pieza.ExistenciaMinima = datos.ExistenciaMinima;
    }
}

public class MovimientoPiezaViewModel
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    /// <summary>Positiva en entradas, negativa en consumos.</summary>
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? Proveedor { get; set; }
    public string? NumeroFactura { get; set; }
    /// <summary>Número de la solicitud de taller en compras directas, consumos y devoluciones.</summary>
    public string? SolicitudTaller { get; set; }
    public string? Placa { get; set; }
}

/// <summary>Kardex de la pieza, el más reciente primero.</summary>
public record FiltroMovimientosPiezaQuery : PaginationFilterQuery<MovimientoPiezaViewModel>
{
    public Guid PiezaId { get; set; }
}

public class FiltroMovimientosPiezaQueryValidator : AbstractValidator<FiltroMovimientosPiezaQuery>
{
    public FiltroMovimientosPiezaQueryValidator()
    {
        Include(new PaginationFilterQueryValidator<MovimientoPiezaViewModel>());
        RuleFor(x => x.PiezaId).NotEmpty();
    }
}

public class GetMovimientosPiezaQueryHandler(IUnitOfWork uow)
    : IRequestHandler<FiltroMovimientosPiezaQuery, Result<PagedData<MovimientoPiezaViewModel>>>
{
    public async Task<Result<PagedData<MovimientoPiezaViewModel>>> Handle(FiltroMovimientosPiezaQuery request, CancellationToken cancellationToken)
    {
        var query = uow.Query<MovimientoPieza>().Where(m => m.PiezaId == request.PiezaId && m.Status);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MovimientoPiezaViewModel
            {
                Id = m.Id,
                Fecha = m.Fecha,
                Concepto = m.Concepto.ToString(),
                Cantidad = m.Concepto == ConceptoMovimientoPieza.Consumo ? -m.Cantidad : m.Cantidad,
                CostoUnitario = m.CostoUnitario,
                Proveedor = m.Proveedor != null ? m.Proveedor.Nombre
                    : m.EntradaPieza != null ? m.EntradaPieza.Proveedor!.Nombre : null,
                NumeroFactura = m.NumeroFactura ?? (m.EntradaPieza != null ? m.EntradaPieza.NumeroFactura : null),
                SolicitudTaller = m.SolicitudTaller != null ? m.SolicitudTaller.Numero : null,
                Placa = m.SolicitudTaller != null ? m.SolicitudTaller.Vehiculo!.Placa : null
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<MovimientoPiezaViewModel>(rows, request.PageSize, request.PageNumber, total));
    }
}

public record GetPiezaQuery(Guid Id) : IRequest<Result<PiezaViewModel>>;

public class GetPiezaQueryHandler(IUnitOfWork uow) : IRequestHandler<GetPiezaQuery, Result<PiezaViewModel>>
{
    public async Task<Result<PiezaViewModel>> Handle(GetPiezaQuery request, CancellationToken cancellationToken)
    {
        var pieza = await uow.Query<Pieza>()
            .Where(p => p.Id == request.Id)
            .Select(p => new PiezaViewModel
            {
                Id = p.Id,
                Codigo = p.Codigo,
                CodigoBarras = p.CodigoBarras,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                UnidadMedida = p.UnidadMedida,
                ExistenciaMinima = p.ExistenciaMinima,
                Activo = p.Status
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (pieza is null) return Result.Failure<PiezaViewModel>(InventarioPiezasErrors.PiezaNotFound(request.Id));

        pieza.Existencia = (await uow.ObtenerExistenciaAsync(pieza.Id, cancellationToken)).Existencia;
        pieza.BajoMinimo = pieza.Existencia < pieza.ExistenciaMinima;
        return Result.Success(pieza);
    }
}
