using Application.Common.DTO;
using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Constants;
using Domain.Entities.Combustible;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Combustible.Tickets;

/// <summary>
/// Línea de tickets iguales: <paramref name="Cantidad"/> tickets de <paramref name="Galones"/> galones cada uno.
/// Los números los genera el sistema en secuencia.
/// </summary>
public record TicketRequest(TipoCombustible TipoCombustible, decimal Galones, int Cantidad = 1);

/// <summary>Solicitud registrada y los números de ticket que se le asignaron, en orden.</summary>
public record SolicitudTicketCreadaViewModel(Guid Id, IReadOnlyList<string> Numeros);

public record CreateSolicitudTicketCommand(
    TipoBeneficiario TipoBeneficiario,
    int? CodCargo,
    string? Cargo,
    Guid? PersonaId,
    Guid SolicitantePersonaId,
    int? CodCargoSolicitante,
    string CargoSolicitante,
    int? CodDependenciaSolicitante,
    string DependenciaSolicitante,
    string Proposito,
    List<TicketRequest> Tickets,
    List<AdjuntoRequest> Adjuntos
) : IRequest<Result<SolicitudTicketCreadaViewModel>>;

public class CreateSolicitudTicketCommandValidator : AbstractValidator<CreateSolicitudTicketCommand>
{
    public const int MaximoTickets = 100;

    public CreateSolicitudTicketCommandValidator()
    {
        this.ValidarBeneficiario(x => x.TipoBeneficiario, x => x.CodCargo, x => x.Cargo, x => x.PersonaId);
        RuleFor(x => x.SolicitantePersonaId).NotEmpty();
        RuleFor(x => x.CargoSolicitante).NotEmpty().MaximumLength(200)
            .WithMessage("Debe indicar el cargo de quien solicita los tickets.");
        RuleFor(x => x.CodCargoSolicitante).GreaterThan(0).When(x => x.CodCargoSolicitante.HasValue);
        this.ValidarDependencia(x => x.CodDependenciaSolicitante, x => x.DependenciaSolicitante);
        RuleFor(x => x.Proposito).NotEmpty().MaximumLength(500);

        RuleFor(x => x.Tickets).NotEmpty().WithMessage("Debe solicitar al menos un ticket.");
        RuleForEach(x => x.Tickets).ChildRules(ticket =>
        {
            ticket.RuleFor(t => t.TipoCombustible).IsInEnum();
            ticket.RuleFor(t => t.Galones).GreaterThan(0).PrecisionScale(10, 2, true);
            ticket.RuleFor(t => t.Cantidad).InclusiveBetween(1, MaximoTickets);
        });
        RuleFor(x => x.Tickets)
            .Must(t => t.Sum(x => x.Cantidad) <= MaximoTickets)
            .When(x => x.Tickets is { Count: > 0 })
            .WithMessage($"Una solicitud no puede tener más de {MaximoTickets} tickets.");

        RuleFor(x => x.Adjuntos)
            .NotEmpty()
            .WithMessage("Debe adjuntar el documento de solicitud de los tickets.");
        RuleForEach(x => x.Adjuntos).SetValidator(new AdjuntoRequestValidator());
    }
}

public class CreateSolicitudTicketCommandHandler(IUnitOfWork uow)
    : IRequestHandler<CreateSolicitudTicketCommand, Result<SolicitudTicketCreadaViewModel>>
{
    public async Task<Result<SolicitudTicketCreadaViewModel>> Handle(CreateSolicitudTicketCommand request, CancellationToken cancellationToken)
    {
        var beneficiario = await uow.ValidarBeneficiarioAsync(request.TipoBeneficiario, request.PersonaId, cancellationToken);
        if (beneficiario.IsFailure) return Result.Failure<SolicitudTicketCreadaViewModel>(beneficiario.Error);

        var solicitante = await uow.ValidarPersonaAsync(request.SolicitantePersonaId, cancellationToken);
        if (solicitante.IsFailure) return Result.Failure<SolicitudTicketCreadaViewModel>(solicitante.Error);

        var esCargo = request.TipoBeneficiario == TipoBeneficiario.Cargo;

        // Los números se reservan en la misma transacción que guarda la solicitud: si algo falla,
        // la secuencia vuelve atrás y no quedan números saltados.
        return await uow.EnTransaccionAsync(async () =>
        {
            var anio = DateTime.Now.Year;
            var cantidad = request.Tickets.Sum(t => t.Cantidad);
            var siguiente = await uow.ReservarSecuenciaAsync(TicketCombustible.CodigoSecuencia, anio, cantidad, cancellationToken);

            var tickets = request.Tickets
                .SelectMany(t => Enumerable.Repeat(t, t.Cantidad))
                .Select(t => new TicketCombustible
                {
                    Numero = TicketCombustible.FormatearNumero(anio, siguiente++),
                    TipoCombustible = t.TipoCombustible,
                    Galones = t.Galones,
                    Estatus = EstatusTicket.Reservado,
                    Status = true
                })
                .ToList();

            var solicitud = new SolicitudTicket
            {
                Id = Guid.NewGuid(),
                Estatus = EstatusSolicitud.Pendiente,
                TipoBeneficiario = request.TipoBeneficiario,
                CodCargo = esCargo ? request.CodCargo : null,
                Cargo = esCargo ? request.Cargo?.Trim() : null,
                PersonaId = esCargo ? null : request.PersonaId,
                SolicitantePersonaId = request.SolicitantePersonaId,
                CodCargoSolicitante = request.CodCargoSolicitante,
                CargoSolicitante = request.CargoSolicitante.Trim(),
                CodDependenciaSolicitante = request.CodDependenciaSolicitante,
                DependenciaSolicitante = request.DependenciaSolicitante.Trim(),
                Proposito = request.Proposito.Trim(),
                Status = true,
                Tickets = tickets
            };

            await uow.AddAsync(solicitud, cancellationToken);
            await uow.AddRangeAsync(
                request.Adjuntos.Select(a => a.ToEntity(TiposOperacion.SolicitudTicket, solicitud.Id)),
                cancellationToken);

            await uow.SaveChangesAsync(cancellationToken);

            return Result.Success(new SolicitudTicketCreadaViewModel(solicitud.Id, tickets.Select(t => t.Numero).ToList()));
        }, cancellationToken);
    }
}
