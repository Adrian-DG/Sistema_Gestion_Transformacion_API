using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Constants;
using Domain.Entities.Historico;
using Domain.Entities.Taller;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Taller.Inventario;

public record LineaEntradaRequest(Guid PiezaId, decimal Cantidad, decimal CostoUnitario);

/// <summary>Registra la compra de piezas al inventario: una factura de un proveedor con sus líneas y costos.</summary>
public record RegistrarEntradaPiezaCommand(
    Guid ProveedorId,
    string? NumeroFactura,
    DateTime? Fecha,
    string? Observacion,
    List<LineaEntradaRequest> Lineas,
    List<AdjuntoRequest>? Adjuntos
) : IRequest<Result<Guid>>;

public class RegistrarEntradaPiezaCommandValidator : AbstractValidator<RegistrarEntradaPiezaCommand>
{
    public RegistrarEntradaPiezaCommandValidator()
    {
        RuleFor(x => x.ProveedorId).NotEmpty().WithMessage("El proveedor es obligatorio.");
        RuleFor(x => x.NumeroFactura).MaximumLength(50);
        RuleFor(x => x.Fecha).LessThanOrEqualTo(_ => DateTime.Now).WithMessage("La fecha no puede ser futura.");
        RuleFor(x => x.Observacion).MaximumLength(500);
        RuleFor(x => x.Lineas).NotEmpty().WithMessage("Agregue al menos una pieza.");
        RuleFor(x => x.Lineas)
            .Must(l => l.Select(x => x.PiezaId).Distinct().Count() == l.Count)
            .When(x => x.Lineas is { Count: > 0 })
            .WithMessage("Cada pieza debe aparecer una sola vez en la entrada.");
        RuleForEach(x => x.Lineas).ChildRules(l =>
        {
            l.RuleFor(x => x.PiezaId).NotEmpty();
            l.RuleFor(x => x.Cantidad).GreaterThan(0).PrecisionScale(12, 2, true);
            l.RuleFor(x => x.CostoUnitario).GreaterThanOrEqualTo(0).PrecisionScale(14, 2, true);
        });
        RuleForEach(x => x.Adjuntos).SetValidator(new AdjuntoRequestValidator());
    }
}

public class RegistrarEntradaPiezaCommandHandler(IUnitOfWork uow) : IRequestHandler<RegistrarEntradaPiezaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarEntradaPiezaCommand request, CancellationToken cancellationToken)
    {
        if (!await uow.Query<Proveedor>().AnyAsync(p => p.Id == request.ProveedorId && p.Status, cancellationToken))
        {
            return Result.Failure<Guid>(InventarioPiezasErrors.ProveedorNotFound(request.ProveedorId));
        }

        var ids = request.Lineas.Select(l => l.PiezaId).ToList();
        var activas = await uow.Query<Pieza>().Where(p => ids.Contains(p.Id) && p.Status).Select(p => p.Id).ToListAsync(cancellationToken);
        var noEncontrada = ids.FirstOrDefault(id => !activas.Contains(id));
        if (noEncontrada != Guid.Empty) return Result.Failure<Guid>(InventarioPiezasErrors.PiezaNotFound(noEncontrada));

        var fecha = request.Fecha ?? DateTime.Now;
        var entrada = new EntradaPieza
        {
            Id = Guid.NewGuid(),
            ProveedorId = request.ProveedorId,
            NumeroFactura = string.IsNullOrWhiteSpace(request.NumeroFactura) ? null : request.NumeroFactura.Trim(),
            Fecha = fecha,
            Observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim(),
            Status = true,
            Lineas = request.Lineas.Select(l => new MovimientoPieza
            {
                Concepto = ConceptoMovimientoPieza.Entrada,
                PiezaId = l.PiezaId,
                Cantidad = l.Cantidad,
                CostoUnitario = l.CostoUnitario,
                Fecha = fecha,
                Status = true
            }).ToList()
        };

        await uow.AddAsync(entrada, cancellationToken);
        if (request.Adjuntos is { Count: > 0 })
        {
            await uow.AddRangeAsync(request.Adjuntos.Select(a => a.ToEntity(TiposOperacion.EntradaPieza, entrada.Id)), cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(entrada.Id);
    }
}

public class EntradaPiezaViewModel
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? NumeroFactura { get; set; }
    public string? Observacion { get; set; }
    public int CantidadLineas { get; set; }
    public decimal Total { get; set; }
    /// <summary>Adjuntos (factura); se descargan con api/taller/adjuntos/{id}.</summary>
    public List<AdjuntoResumenViewModel> Adjuntos { get; set; } = [];
    public List<LineaEntradaViewModel> Lineas { get; set; } = [];
}

public class LineaEntradaViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Pieza { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class AdjuntoResumenViewModel
{
    public Guid Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>Entradas al inventario, la más reciente primero; busca por proveedor, factura o pieza.</summary>
public record FiltroEntradasPiezaQuery : PaginationFilterQuery<EntradaPiezaViewModel>;

public class FiltroEntradasPiezaQueryValidator : AbstractValidator<FiltroEntradasPiezaQuery>
{
    public FiltroEntradasPiezaQueryValidator() => Include(new PaginationFilterQueryValidator<EntradaPiezaViewModel>());
}

public class GetEntradasPiezaQueryHandler(IUnitOfWork uow)
    : IRequestHandler<FiltroEntradasPiezaQuery, Result<PagedData<EntradaPiezaViewModel>>>
{
    public async Task<Result<PagedData<EntradaPiezaViewModel>>> Handle(FiltroEntradasPiezaQuery request, CancellationToken cancellationToken)
    {
        var adjuntos = uow.Query<Adjunto>().Where(a => a.TipoOperacionId == TiposOperacion.EntradaPieza && a.Status);
        var query = uow.Query<EntradaPieza>().Where(e => e.Status);

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            var texto = request.Param.Trim();
            query = query.Where(e =>
                e.Proveedor!.Nombre.Contains(texto) ||
                (e.NumeroFactura != null && e.NumeroFactura.Contains(texto)) ||
                e.Lineas.Any(l => l.Pieza!.Nombre.Contains(texto) || l.Pieza.Codigo.Contains(texto)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(e => e.Fecha)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EntradaPiezaViewModel
            {
                Id = e.Id,
                Fecha = e.Fecha,
                Proveedor = e.Proveedor!.Nombre,
                NumeroFactura = e.NumeroFactura,
                Observacion = e.Observacion,
                CantidadLineas = e.Lineas.Count,
                Total = e.Lineas.Sum(l => l.Cantidad * l.CostoUnitario),
                Adjuntos = adjuntos
                    .Where(a => a.IdentificadorOperacion == e.Id)
                    .Select(a => new AdjuntoResumenViewModel { Id = a.Id, NombreArchivo = a.NombreArchivo, ContentType = a.ContentType })
                    .ToList(),
                Lineas = e.Lineas
                    .OrderBy(l => l.Pieza!.Nombre)
                    .Select(l => new LineaEntradaViewModel
                    {
                        Codigo = l.Pieza!.Codigo,
                        Pieza = l.Pieza.Nombre,
                        Cantidad = l.Cantidad,
                        CostoUnitario = l.CostoUnitario
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<EntradaPiezaViewModel>(rows, request.PageSize, request.PageNumber, total));
    }
}
