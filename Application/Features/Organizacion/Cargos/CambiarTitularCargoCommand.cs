using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Organizacion;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Organizacion.Cargos;

/// <summary>
/// Cambia la persona que ocupa el cargo. Las asignaciones abiertas del cargo se mantienen
/// y el titular anterior queda en el histórico. <paramref name="PersonaId"/> nulo deja el cargo vacante.
/// </summary>
public record CambiarTitularCargoCommand(Guid CargoId, Guid? PersonaId, DateTime? FechaCambio) : IRequest<Result>;

public class CambiarTitularCargoCommandValidator : AbstractValidator<CambiarTitularCargoCommand>
{
    public CambiarTitularCargoCommandValidator()
    {
        RuleFor(x => x.CargoId).NotEmpty();
        RuleFor(x => x.FechaCambio).LessThanOrEqualTo(_ => DateTime.Now)
            .WithMessage("La fecha del cambio no puede ser futura.");
    }
}

public class CambiarTitularCargoCommandHandler(IUnitOfWork uow) : IRequestHandler<CambiarTitularCargoCommand, Result>
{
    public async Task<Result> Handle(CambiarTitularCargoCommand request, CancellationToken cancellationToken)
    {
        var cargo = await uow.Tracked<Cargo>()
            .Include(c => c.HistorialTitulares.Where(h => h.FechaHasta == null))
            .FirstOrDefaultAsync(c => c.Id == request.CargoId, cancellationToken);

        if (cargo is null)
        {
            return Result.Failure(OrganizacionErrors.CargoNotFound(request.CargoId));
        }

        if (request.PersonaId.HasValue)
        {
            var persona = await uow.ValidarPersonaAsync(request.PersonaId.Value, cancellationToken);
            if (persona.IsFailure) return persona;
        }

        cargo.CambiarTitular(request.PersonaId, request.FechaCambio ?? DateTime.Now);
        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
