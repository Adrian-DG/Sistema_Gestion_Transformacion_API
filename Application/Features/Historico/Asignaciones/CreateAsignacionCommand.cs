using Application.Common.DTO;
using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Constants;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Historico.Asignaciones;

/// <summary>
/// Registra la solicitud de un vehículo. Queda en estatus Pendiente hasta su aprobación;
/// mientras tanto el vehículo sigue disponible.
/// </summary>
public record CreateAsignacionCommand(
    TipoBeneficiario TipoAsignacion,
    Guid? CargoId,
    Guid? PersonaId,
    Guid SolicitantePersonaId,
    Guid DependenciaSolicitanteId,
    Guid VehiculoId,
    string Proposito,
    string? Destino,
    DateTime FechaSalida,
    DateTime FechaRetornoEstimada,
    List<AdjuntoRequest> Adjuntos
) : IRequest<Result<Guid>>;

public class CreateAsignacionCommandValidator : AbstractValidator<CreateAsignacionCommand>
{
    public CreateAsignacionCommandValidator()
    {
        this.ValidarBeneficiario(x => x.TipoAsignacion, x => x.CargoId, x => x.PersonaId);
        RuleFor(x => x.SolicitantePersonaId).NotEmpty();
        RuleFor(x => x.DependenciaSolicitanteId).NotEmpty();
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.Proposito).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Destino).MaximumLength(250);
        RuleFor(x => x.FechaRetornoEstimada)
            .GreaterThan(x => x.FechaSalida)
            .WithMessage("La fecha de retorno estimada debe ser posterior a la fecha de salida.");
        RuleFor(x => x.Adjuntos)
            .NotNull()
            .NotEmpty()
            .WithMessage("Debe adjuntar al menos un documento para registrar la asignación.");
        RuleForEach(x => x.Adjuntos).SetValidator(new AdjuntoRequestValidator());
    }
}

public class CreateAsignacionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateAsignacionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAsignacionCommand request, CancellationToken cancellationToken)
    {
        var beneficiario = await unitOfWork.ValidarBeneficiarioAsync(request.TipoAsignacion, request.CargoId, request.PersonaId, cancellationToken);
        if (beneficiario.IsFailure) return Result.Failure<Guid>(beneficiario.Error);

        var solicitante = await unitOfWork.ValidarSolicitanteAsync(request.SolicitantePersonaId, request.DependenciaSolicitanteId, cancellationToken);
        if (solicitante.IsFailure) return Result.Failure<Guid>(solicitante.Error);

        var vehiculoResult = await unitOfWork.VehiculoRepository.GetById(request.VehiculoId, cancellationToken);
        if (vehiculoResult.IsFailure) return Result.Failure<Guid>(vehiculoResult.Error);

        // Disponible indica si el vehículo está operativo (no fuera de servicio).
        if (!vehiculoResult.Value.Disponible)
        {
            return Result.Failure<Guid>(VehiculoErrors.NotAvailable(request.VehiculoId));
        }

        var ocupado = await unitOfWork.VehiculoOcupadoAsync(
            request.VehiculoId, request.FechaSalida, request.FechaRetornoEstimada, null, DateTime.Now, cancellationToken);

        if (ocupado)
        {
            return Result.Failure<Guid>(AsignacionErrors.VehiculoOcupado(request.VehiculoId));
        }

        var asignacion = new Asignacion
        {
            Id = Guid.NewGuid(),
            Estatus = EstatusSolicitud.Pendiente,
            TipoAsignacion = request.TipoAsignacion,
            CargoId = request.CargoId,
            PersonaId = request.PersonaId,
            SolicitantePersonaId = request.SolicitantePersonaId,
            DependenciaSolicitanteId = request.DependenciaSolicitanteId,
            VehiculoId = request.VehiculoId,
            Proposito = request.Proposito,
            Destino = request.Destino,
            FechaSalida = request.FechaSalida,
            FechaRetornoEstimada = request.FechaRetornoEstimada,
            Status = true
        };

        await unitOfWork.AsignacionRepository.Create(asignacion, cancellationToken);
        await unitOfWork.AddRangeAsync(
            request.Adjuntos.Select(a => a.ToEntity(TiposOperacion.Asignacion, asignacion.Id)),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(asignacion.Id);
    }
}
