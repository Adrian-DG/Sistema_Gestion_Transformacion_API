using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Combustible;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Tickets;

/// <summary>Aprueba la solicitud; sus tickets pasan a Disponible y comprometen la existencia.</summary>
public record AprobarSolicitudTicketCommand(Guid SolicitudId) : IRequest<Result>;

public record RechazarSolicitudTicketCommand(Guid SolicitudId, string Motivo) : IRequest<Result>;

/// <summary>Anula la solicitud por un error de digitación o un documento equivocado.</summary>
public record CancelarSolicitudTicketCommand(Guid SolicitudId, string Motivo) : IRequest<Result>;

public class RechazarSolicitudTicketCommandValidator : AbstractValidator<RechazarSolicitudTicketCommand>
{
    public RechazarSolicitudTicketCommandValidator() => RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
}

public class CancelarSolicitudTicketCommandValidator : AbstractValidator<CancelarSolicitudTicketCommand>
{
    public CancelarSolicitudTicketCommandValidator() => RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
}

public class CambiarEstatusSolicitudTicketCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser) :
    IRequestHandler<AprobarSolicitudTicketCommand, Result>,
    IRequestHandler<RechazarSolicitudTicketCommand, Result>,
    IRequestHandler<CancelarSolicitudTicketCommand, Result>
{
    public Task<Result> Handle(AprobarSolicitudTicketCommand request, CancellationToken cancellationToken) =>
        Execute(request.SolicitudId, async solicitud =>
        {
            var galonesPorTipo = solicitud.Tickets
                .GroupBy(t => t.TipoCombustible)
                .Select(g => new { Tipo = g.Key, Galones = g.Sum(t => t.Galones) });

            foreach (var item in galonesPorTipo)
            {
                var existencia = await uow.ObtenerExistenciaAsync(item.Tipo, cancellationToken);
                if (existencia.DisponibleParaDespacho < item.Galones)
                {
                    return Result.Failure(CombustibleErrors.ExistenciaInsuficiente(
                        item.Tipo.ToString(), existencia.DisponibleParaDespacho, item.Galones));
                }
            }

            return solicitud.Aprobar(currentUser.UserId ?? Guid.Empty, DateTime.Now);
        }, cancellationToken);

    public Task<Result> Handle(RechazarSolicitudTicketCommand request, CancellationToken cancellationToken) =>
        Execute(request.SolicitudId, s => Task.FromResult(s.Rechazar(request.Motivo)), cancellationToken);

    public Task<Result> Handle(CancelarSolicitudTicketCommand request, CancellationToken cancellationToken) =>
        Execute(request.SolicitudId, s => Task.FromResult(s.Cancelar(request.Motivo)), cancellationToken);

    private async Task<Result> Execute(Guid solicitudId, Func<SolicitudTicket, Task<Result>> action, CancellationToken cancellationToken)
    {
        var solicitud = await uow.Tracked<SolicitudTicket>()
            .Include(s => s.Tickets)
            .FirstOrDefaultAsync(s => s.Id == solicitudId, cancellationToken);

        if (solicitud is null)
        {
            return Result.Failure(SolicitudTicketErrors.NotFound(solicitudId));
        }

        var result = await action(solicitud);
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
