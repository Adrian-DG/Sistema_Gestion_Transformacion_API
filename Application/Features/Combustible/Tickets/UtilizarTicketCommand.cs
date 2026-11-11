using Application.Common.Validation;
using Application.Contracts;
using Application.Contracts.Usuarios;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Combustible;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Tickets;

/// <summary>
/// Despacha (canjea) un ticket: lo marca como utilizado y registra la salida de combustible con la fecha
/// y hora del servidor y el operador que lo despachó. <paramref name="Codigo"/> es lo leído del código de
/// barras (número y código de verificación) o el número escrito a mano con su código de verificación.
/// Quien presenta el ticket no tiene que ser a quien se le entregó.
/// </summary>
/// <remarks>
/// <paramref name="FechaUso"/> solo se usa para registrar después un canje que no se pudo escanear en el
/// momento; el módulo de despacho no la envía.
/// </remarks>
public record UtilizarTicketCommand(
    string Codigo,
    DateTime? FechaUso,
    Guid? ReceptorPersonaId,
    string? ReceptorNombre
) : IRequest<Result<DespachoTicketViewModel>>;

public class UtilizarTicketCommandValidator : AbstractValidator<UtilizarTicketCommand>
{
    public UtilizarTicketCommandValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(50).WithMessage("Escanee el ticket o escriba su número.");
        RuleFor(x => x.FechaUso).LessThanOrEqualTo(_ => DateTime.Now).WithMessage("La fecha de uso no puede ser futura.");
        RuleFor(x => x.ReceptorNombre).MaximumLength(200);
    }
}

public class UtilizarTicketCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IUsuarioService usuarios)
    : IRequestHandler<UtilizarTicketCommand, Result<DespachoTicketViewModel>>
{
    public async Task<Result<DespachoTicketViewModel>> Handle(UtilizarTicketCommand request, CancellationToken cancellationToken)
    {
        var (numero, codigo) = TicketCombustible.InterpretarCodigo(request.Codigo);
        var ticket = await uow.Tracked<TicketCombustible>()
            .Include(t => t.SolicitudTicket!).ThenInclude(s => s.Persona)
            .FirstOrDefaultAsync(t => t.Numero == numero && t.Estatus != EstatusTicket.Anulado, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<DespachoTicketViewModel>(TicketCombustibleErrors.NotFound(numero));
        }

        var verificacion = VerificacionTicket.Validar(ticket.CodigoVerificacion, codigo, numero);
        if (verificacion.IsFailure) return Result.Failure<DespachoTicketViewModel>(verificacion.Error);

        if (request.ReceptorPersonaId.HasValue)
        {
            var receptor = await uow.ValidarPersonaAsync(request.ReceptorPersonaId.Value, cancellationToken);
            if (receptor.IsFailure) return Result.Failure<DespachoTicketViewModel>(receptor.Error);
        }

        var fechaUso = request.FechaUso ?? DateTime.Now;
        var uso = ticket.Utilizar(fechaUso, currentUser.UserId);
        if (uso.IsFailure) return Result.Failure<DespachoTicketViewModel>(uso.Error);

        // Los galones del ticket ya estaban comprometidos al aprobarlo; solo se valida la existencia física.
        var existencia = await uow.ObtenerExistenciaAsync(ticket.TipoCombustible, cancellationToken);
        if (existencia.Existencia < ticket.Galones)
        {
            return Result.Failure<DespachoTicketViewModel>(CombustibleErrors.ExistenciaInsuficiente(
                ticket.TipoCombustible.ToString(), existencia.Existencia, ticket.Galones));
        }

        var receptorNombre = string.IsNullOrWhiteSpace(request.ReceptorNombre) ? null : request.ReceptorNombre.Trim();
        var movimiento = new MovimientoCombustible
        {
            Concepto = ConceptoMovimientoCombustible.Ticket,
            TipoCombustible = ticket.TipoCombustible,
            Galones = ticket.Galones,
            Fecha = fechaUso,
            TicketCombustibleId = ticket.Id,
            ReceptorPersonaId = request.ReceptorPersonaId,
            ReceptorNombre = receptorNombre,
            Status = true
        };

        await uow.AddAsync(movimiento, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        var operador = currentUser.UserId is { } usuarioId
            ? (await usuarios.GetNombresAsync([usuarioId], cancellationToken)).GetValueOrDefault(usuarioId)
            : null;

        var solicitud = ticket.SolicitudTicket!;
        return Result.Success(new DespachoTicketViewModel
        {
            MovimientoId = movimiento.Id,
            Numero = ticket.Numero,
            TipoCombustible = ticket.TipoCombustible.ToString(),
            Galones = ticket.Galones,
            FechaUso = fechaUso,
            EntregadoA = solicitud.Cargo ?? (solicitud.Persona is { } p ? $"{p.Apellido} {p.Nombre}" : null),
            ReceptorNombre = receptorNombre,
            DespachadoPor = operador
        });
    }
}

/// <summary>Comprobación del código de verificación leído contra el del ticket.</summary>
public static class VerificacionTicket
{
    /// <summary>
    /// Los tickets anteriores al código de barras (sin código) se aceptan solo con el número; los demás
    /// exigen el código, que debe coincidir.
    /// </summary>
    public static Result Validar(string? codigoTicket, string? codigoLeido, string numero)
    {
        if (codigoTicket is null) return Result.Success();
        if (codigoLeido is null) return Result.Failure(TicketCombustibleErrors.CodigoRequerido(numero));
        return string.Equals(codigoTicket, codigoLeido, StringComparison.OrdinalIgnoreCase)
            ? Result.Success()
            : Result.Failure(TicketCombustibleErrors.CodigoInvalido(numero));
    }
}
