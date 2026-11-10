using Application.Common.DTO;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Constants;
using Domain.Entities.Combustible;
using Domain.Entities.Taller;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Movimientos;

/// <summary>
/// Registra una entrada (recepción) de combustible de un suplidor registrado en el sistema
/// (<see cref="Domain.Entities.Taller.Proveedor"/> activo de tipo SuplidorCombustible). Se
/// guarda también su nombre a la fecha.
/// </summary>
public record RegistrarCompraCommand(
    TipoCombustible TipoCombustible,
    decimal Galones,
    DateTime? Fecha,
    Guid ProveedorId,
    string? NumeroFactura,
    string? Observacion,
    List<AdjuntoRequest>? Adjuntos
) : IRequest<Result<Guid>>;

public class RegistrarCompraCommandValidator : AbstractValidator<RegistrarCompraCommand>
{
    public RegistrarCompraCommandValidator()
    {
        RuleFor(x => x.TipoCombustible).IsInEnum();
        RuleFor(x => x.Galones).GreaterThan(0).PrecisionScale(12, 2, true);
        RuleFor(x => x.Fecha).LessThanOrEqualTo(_ => DateTime.Now).WithMessage("La fecha no puede ser futura.");
        RuleFor(x => x.ProveedorId).NotEmpty().WithMessage("Seleccione el suplidor del combustible.");
        RuleFor(x => x.NumeroFactura).MaximumLength(50);
        RuleFor(x => x.Observacion).MaximumLength(500);
        RuleForEach(x => x.Adjuntos).SetValidator(new AdjuntoRequestValidator());
    }
}

public class RegistrarCompraCommandHandler(IUnitOfWork uow) : IRequestHandler<RegistrarCompraCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarCompraCommand request, CancellationToken cancellationToken)
    {
        var suplidor = await uow.Query<Proveedor>()
            .Where(p => p.Id == request.ProveedorId && p.Status)
            .Select(p => new { p.Nombre, p.Tipo })
            .FirstOrDefaultAsync(cancellationToken);
        if (suplidor is null) return Result.Failure<Guid>(InventarioPiezasErrors.ProveedorNotFound(request.ProveedorId));
        if (suplidor.Tipo != TipoProveedor.SuplidorCombustible) return Result.Failure<Guid>(CombustibleErrors.NoEsSuplidorCombustible(suplidor.Nombre));
        var proveedor = suplidor.Nombre;

        var movimiento = new MovimientoCombustible
        {
            Id = Guid.NewGuid(),
            Concepto = ConceptoMovimientoCombustible.Compra,
            TipoCombustible = request.TipoCombustible,
            Galones = request.Galones,
            Fecha = request.Fecha ?? DateTime.Now,
            ProveedorId = request.ProveedorId,
            Proveedor = proveedor,
            NumeroFactura = request.NumeroFactura,
            Proposito = request.Observacion,
            Status = true
        };

        await uow.AddAsync(movimiento, cancellationToken);

        if (request.Adjuntos is { Count: > 0 })
        {
            await uow.AddRangeAsync(
                request.Adjuntos.Select(a => a.ToEntity(TiposOperacion.MovimientoCombustible, movimiento.Id)),
                cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(movimiento.Id);
    }
}
