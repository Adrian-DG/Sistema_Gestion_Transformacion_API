using Application.Common.DTO;
using Application.Contracts;
using Domain.Common;
using Domain.Constants;
using Domain.Entities.Combustible;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Combustible.Movimientos;

/// <summary>Registra una entrada de combustible.</summary>
public record RegistrarCompraCommand(
    TipoCombustible TipoCombustible,
    decimal Galones,
    DateTime? Fecha,
    string Proveedor,
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
        RuleFor(x => x.Proveedor).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NumeroFactura).MaximumLength(50);
        RuleFor(x => x.Observacion).MaximumLength(500);
        RuleForEach(x => x.Adjuntos).SetValidator(new AdjuntoRequestValidator());
    }
}

public class RegistrarCompraCommandHandler(IUnitOfWork uow) : IRequestHandler<RegistrarCompraCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarCompraCommand request, CancellationToken cancellationToken)
    {
        var movimiento = new MovimientoCombustible
        {
            Id = Guid.NewGuid(),
            Concepto = ConceptoMovimientoCombustible.Compra,
            TipoCombustible = request.TipoCombustible,
            Galones = request.Galones,
            Fecha = request.Fecha ?? DateTime.Now,
            Proveedor = request.Proveedor,
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
