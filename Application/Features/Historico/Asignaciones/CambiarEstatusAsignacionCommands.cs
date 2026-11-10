using Application.Contracts;
using Application.Security;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico.Asignaciones;

/// <summary>
/// Aprueba la asignación y reserva el vehículo. Vuelve a aplicar las reglas de disponibilidad (pudo cambiar
/// desde el registro); con advertencias responde Asignacion.RequiereConfirmacion hasta que se envíe
/// <paramref name="ConfirmarAdvertencias"/>.
/// </summary>
public record AprobarAsignacionCommand(Guid AsignacionId, bool ConfirmarAdvertencias = false) : IRequest<Result>;

public record RechazarAsignacionCommand(Guid AsignacionId, string Motivo) : IRequest<Result>;

/// <summary>Anula la asignación por un error de digitación o un documento equivocado.</summary>
public record CancelarAsignacionCommand(Guid AsignacionId, string Motivo) : IRequest<Result>;

/// <summary>Registra la entrada del vehículo a la base y finaliza la asignación.</summary>
public record RegistrarRetornoAsignacionCommand(Guid AsignacionId, DateTime FechaRetorno) : IRequest<Result>;

public class RechazarAsignacionCommandValidator : AbstractValidator<RechazarAsignacionCommand>
{
    public RechazarAsignacionCommandValidator() => RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
}

public class CancelarAsignacionCommandValidator : AbstractValidator<CancelarAsignacionCommand>
{
    public CancelarAsignacionCommandValidator() => RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
}

public class RegistrarRetornoAsignacionCommandValidator : AbstractValidator<RegistrarRetornoAsignacionCommand>
{
    public RegistrarRetornoAsignacionCommandValidator() =>
        RuleFor(x => x.FechaRetorno).LessThanOrEqualTo(_ => DateTime.Now)
            .WithMessage("La fecha de retorno no puede ser futura.");
}

public class CambiarEstatusAsignacionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser) :
    IRequestHandler<AprobarAsignacionCommand, Result>,
    IRequestHandler<RechazarAsignacionCommand, Result>,
    IRequestHandler<CancelarAsignacionCommand, Result>,
    IRequestHandler<RegistrarRetornoAsignacionCommand, Result>
{
    public Task<Result> Handle(AprobarAsignacionCommand request, CancellationToken cancellationToken) =>
        Execute(request.AsignacionId, async asignacion =>
        {
            // Quien registra no aprueba (salvo el Administrador).
            if (asignacion.CreatedBy == currentUser.UserId && !currentUser.TienePermiso(AppPermission.Administrador))
            {
                return Result.Failure(AsignacionErrors.AutoAprobacion);
            }

            if (asignacion.FechaRetornoEstimada <= DateTime.Now)
            {
                return Result.Failure(AsignacionErrors.RetornoVencido(asignacion.FechaRetornoEstimada));
            }

            // El vehículo pudo entrar al taller, asignarse o darse de baja mientras la solicitud estaba pendiente.
            var disponible = await uow.ValidarAsignacionAsync(
                asignacion.VehiculoId, asignacion.ChoferPersonaId, asignacion.FechaSalida, asignacion.FechaRetornoEstimada,
                asignacion.Id, request.ConfirmarAdvertencias, cancellationToken);
            if (disponible.IsFailure) return disponible;

            return asignacion.Aprobar(currentUser.UserId ?? Guid.Empty, DateTime.Now);
        }, cancellationToken);

    public Task<Result> Handle(RechazarAsignacionCommand request, CancellationToken cancellationToken) =>
        Execute(request.AsignacionId, a => Task.FromResult(a.Rechazar(request.Motivo)), cancellationToken);

    public Task<Result> Handle(CancelarAsignacionCommand request, CancellationToken cancellationToken) =>
        Execute(request.AsignacionId, a => Task.FromResult(a.Cancelar(request.Motivo)), cancellationToken);

    public Task<Result> Handle(RegistrarRetornoAsignacionCommand request, CancellationToken cancellationToken) =>
        Execute(request.AsignacionId, a => Task.FromResult(a.RegistrarRetorno(request.FechaRetorno)), cancellationToken);

    private async Task<Result> Execute(Guid asignacionId, Func<Asignacion, Task<Result>> action, CancellationToken cancellationToken)
    {
        var asignacion = await uow.Tracked<Asignacion>().FirstOrDefaultAsync(a => a.Id == asignacionId, cancellationToken);

        if (asignacion is null)
        {
            return Result.Failure(AsignacionErrors.NotFound(asignacionId));
        }

        var result = await action(asignacion);
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
