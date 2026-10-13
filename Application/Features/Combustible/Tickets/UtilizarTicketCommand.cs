using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Combustible;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Tickets;

/// <summary>
/// Canjea un ticket por su número: lo marca como utilizado y registra la salida de combustible.
/// Quien presenta el ticket no tiene que ser a quien se le entregó.
/// </summary>
public record UtilizarTicketCommand(
    string Numero,
    DateTime? FechaUso,
    Guid? ReceptorPersonaId,
    string? ReceptorNombre
) : IRequest<Result<Guid>>;

public class UtilizarTicketCommandValidator : AbstractValidator<UtilizarTicketCommand>
{
    public UtilizarTicketCommandValidator()
    {
        RuleFor(x => x.Numero).NotEmpty();
        RuleFor(x => x.FechaUso).LessThanOrEqualTo(_ => DateTime.Now).WithMessage("La fecha de uso no puede ser futura.");
        RuleFor(x => x.ReceptorNombre).MaximumLength(200);
    }
}

public class UtilizarTicketCommandHandler(IUnitOfWork uow) : IRequestHandler<UtilizarTicketCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UtilizarTicketCommand request, CancellationToken cancellationToken)
    {
        var numero = request.Numero.Trim();
        var ticket = await uow.Tracked<TicketCombustible>()
            .FirstOrDefaultAsync(t => t.Numero == numero && t.Estatus != EstatusTicket.Anulado, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<Guid>(TicketCombustibleErrors.NotFound(numero));
        }

        if (request.ReceptorPersonaId.HasValue)
        {
            var receptor = await uow.ValidarPersonaAsync(request.ReceptorPersonaId.Value, cancellationToken);
            if (receptor.IsFailure) return Result.Failure<Guid>(receptor.Error);
        }

        var fechaUso = request.FechaUso ?? DateTime.Now;
        var uso = ticket.Utilizar(fechaUso);
        if (uso.IsFailure) return Result.Failure<Guid>(uso.Error);

        // Los galones del ticket ya estaban comprometidos al aprobarlo; solo se valida la existencia física.
        var existencia = await uow.ObtenerExistenciaAsync(ticket.TipoCombustible, cancellationToken);
        if (existencia.Existencia < ticket.Galones)
        {
            return Result.Failure<Guid>(CombustibleErrors.ExistenciaInsuficiente(
                ticket.TipoCombustible.ToString(), existencia.Existencia, ticket.Galones));
        }

        var movimiento = new MovimientoCombustible
        {
            Concepto = ConceptoMovimientoCombustible.Ticket,
            TipoCombustible = ticket.TipoCombustible,
            Galones = ticket.Galones,
            Fecha = fechaUso,
            TicketCombustibleId = ticket.Id,
            ReceptorPersonaId = request.ReceptorPersonaId,
            ReceptorNombre = request.ReceptorNombre,
            Status = true
        };

        await uow.AddAsync(movimiento, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(movimiento.Id);
    }
}
