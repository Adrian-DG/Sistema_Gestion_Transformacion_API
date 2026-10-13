using Application.Common.DTO;
using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Constants;
using Domain.Entities.Combustible;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Tickets;

/// <summary>Ticket pre-impreso: se digita su número, tipo de combustible y galones.</summary>
public record TicketRequest(string Numero, TipoCombustible TipoCombustible, decimal Galones);

public record CreateSolicitudTicketCommand(
    TipoBeneficiario TipoBeneficiario,
    Guid? CargoId,
    Guid? PersonaId,
    Guid SolicitantePersonaId,
    Guid DependenciaSolicitanteId,
    string Proposito,
    List<TicketRequest> Tickets,
    List<AdjuntoRequest> Adjuntos
) : IRequest<Result<Guid>>;

public class CreateSolicitudTicketCommandValidator : AbstractValidator<CreateSolicitudTicketCommand>
{
    public CreateSolicitudTicketCommandValidator()
    {
        this.ValidarBeneficiario(x => x.TipoBeneficiario, x => x.CargoId, x => x.PersonaId);
        RuleFor(x => x.SolicitantePersonaId).NotEmpty();
        RuleFor(x => x.DependenciaSolicitanteId).NotEmpty();
        RuleFor(x => x.Proposito).NotEmpty().MaximumLength(500);

        RuleFor(x => x.Tickets).NotEmpty().WithMessage("Debe registrar al menos un ticket.");
        RuleForEach(x => x.Tickets).ChildRules(ticket =>
        {
            ticket.RuleFor(t => t.Numero).NotEmpty().MaximumLength(30);
            ticket.RuleFor(t => t.TipoCombustible).IsInEnum();
            ticket.RuleFor(t => t.Galones).GreaterThan(0).PrecisionScale(10, 2, true);
        });
        RuleFor(x => x.Tickets)
            .Must(t => t.Select(x => x.Numero.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == t.Count)
            .When(x => x.Tickets is { Count: > 0 })
            .WithMessage("Hay números de ticket repetidos en la solicitud.");

        RuleFor(x => x.Adjuntos)
            .NotEmpty()
            .WithMessage("Debe adjuntar el documento de solicitud de los tickets.");
        RuleForEach(x => x.Adjuntos).SetValidator(new AdjuntoRequestValidator());
    }
}

public class CreateSolicitudTicketCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateSolicitudTicketCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSolicitudTicketCommand request, CancellationToken cancellationToken)
    {
        var beneficiario = await uow.ValidarBeneficiarioAsync(request.TipoBeneficiario, request.CargoId, request.PersonaId, cancellationToken);
        if (beneficiario.IsFailure) return Result.Failure<Guid>(beneficiario.Error);

        var solicitante = await uow.ValidarSolicitanteAsync(request.SolicitantePersonaId, request.DependenciaSolicitanteId, cancellationToken);
        if (solicitante.IsFailure) return Result.Failure<Guid>(solicitante.Error);

        var numeros = request.Tickets.Select(t => t.Numero.Trim()).ToList();
        var duplicados = await uow.Query<TicketCombustible>()
            .Where(t => numeros.Contains(t.Numero) && t.Estatus != EstatusTicket.Anulado)
            .Select(t => t.Numero)
            .ToListAsync(cancellationToken);

        if (duplicados.Count > 0)
        {
            return Result.Failure<Guid>(SolicitudTicketErrors.NumerosDuplicados(duplicados));
        }

        var solicitud = new SolicitudTicket
        {
            Id = Guid.NewGuid(),
            Estatus = EstatusSolicitud.Pendiente,
            TipoBeneficiario = request.TipoBeneficiario,
            CargoId = request.CargoId,
            PersonaId = request.PersonaId,
            SolicitantePersonaId = request.SolicitantePersonaId,
            DependenciaSolicitanteId = request.DependenciaSolicitanteId,
            Proposito = request.Proposito,
            Status = true,
            Tickets = request.Tickets.Select(t => new TicketCombustible
            {
                Numero = t.Numero.Trim(),
                TipoCombustible = t.TipoCombustible,
                Galones = t.Galones,
                Estatus = EstatusTicket.Reservado,
                Status = true
            }).ToList()
        };

        await uow.AddAsync(solicitud, cancellationToken);
        await uow.AddRangeAsync(
            request.Adjuntos.Select(a => a.ToEntity(TiposOperacion.SolicitudTicket, solicitud.Id)),
            cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(solicitud.Id);
    }
}
