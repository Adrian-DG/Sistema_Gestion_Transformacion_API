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
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico.Asignaciones;

/// <summary>
/// Registra la solicitud de un vehículo. Queda en estatus Pendiente hasta su aprobación;
/// mientras tanto el vehículo sigue disponible.
/// </summary>
/// <remarks>
/// Las personas (solicitante, asignado y chofer) se identifican por su registro en el sistema, que guarda
/// la cédula y el rango; el cargo y la dependencia del solicitante se toman de SIPFFAA (código y nombre).
/// </remarks>
public record CreateAsignacionCommand(
    Guid SolicitantePersonaId,
    int? CodCargoSolicitante,
    string CargoSolicitante,
    int? CodDependenciaSolicitante,
    string DependenciaSolicitante,
    TipoBeneficiario TipoAsignacion,
    Guid PersonaId,
    int? CodCargo,
    string? Cargo,
    Guid ChoferPersonaId,
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
        RuleFor(x => x.SolicitantePersonaId).NotEmpty().WithMessage("Debe indicar quién solicita el vehículo.");
        RuleFor(x => x.CargoSolicitante).NotEmpty().MaximumLength(200)
            .WithMessage("Debe indicar el cargo de quien solicita el vehículo.");
        RuleFor(x => x.CodCargoSolicitante).GreaterThan(0).When(x => x.CodCargoSolicitante.HasValue);
        this.ValidarDependencia(x => x.CodDependenciaSolicitante, x => x.DependenciaSolicitante);

        RuleFor(x => x.TipoAsignacion).IsInEnum();
        RuleFor(x => x.PersonaId).NotEmpty().WithMessage("Debe indicar a quién se asigna el vehículo.");
        RuleFor(x => x.Cargo).MaximumLength(200);
        When(x => x.TipoAsignacion == TipoBeneficiario.Cargo, () =>
        {
            RuleFor(x => x.CodCargo).NotNull().GreaterThan(0).WithMessage("Para una asignación al cargo debe indicar CodCargo.");
            RuleFor(x => x.Cargo).NotEmpty().WithMessage("Para una asignación al cargo debe indicar el cargo.");
        });
        When(x => x.TipoAsignacion == TipoBeneficiario.Persona, () =>
        {
            RuleFor(x => x).Must(x => x.CodCargo is null && x.Cargo is null)
                .WithMessage("Una asignación a título personal no lleva CodCargo ni Cargo.");
        });

        RuleFor(x => x.ChoferPersonaId).NotEmpty().WithMessage("Debe indicar el chofer asignado.");
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
        // El asignado puede ser el mismo solicitante, y el chofer, cualquiera de los dos.
        var ids = new[] { request.SolicitantePersonaId, request.PersonaId, request.ChoferPersonaId };
        var rangos = await unitOfWork.Query<Persona>()
            .Where(p => ids.Contains(p.Id) && p.Status)
            .Select(p => new { p.Id, Rango = p.Rango != null ? p.Rango.Nombre : null })
            .ToDictionaryAsync(p => p.Id, p => p.Rango, cancellationToken);

        var noEncontrada = ids.FirstOrDefault(id => !rangos.ContainsKey(id));
        if (noEncontrada != Guid.Empty)
        {
            return Result.Failure<Guid>(PersonaErrors.NotFound(noEncontrada));
        }

        var vehiculoResult = await unitOfWork.VehiculoRepository.GetById(request.VehiculoId, cancellationToken);
        if (vehiculoResult.IsFailure) return Result.Failure<Guid>(vehiculoResult.Error);

        // En mantenimiento o inactivo no se asigna; las misiones se validan por fechas a continuación.
        if (vehiculoResult.Value.Estado != EstadoVehiculo.Operativo)
        {
            return Result.Failure<Guid>(VehiculoErrors.NoOperativo(request.VehiculoId, vehiculoResult.Value.Estado));
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
            SolicitantePersonaId = request.SolicitantePersonaId,
            RangoSolicitante = rangos[request.SolicitantePersonaId],
            CodCargoSolicitante = request.CodCargoSolicitante,
            CargoSolicitante = request.CargoSolicitante.Trim(),
            CodDependenciaSolicitante = request.CodDependenciaSolicitante,
            DependenciaSolicitante = request.DependenciaSolicitante.Trim(),
            TipoAsignacion = request.TipoAsignacion,
            PersonaId = request.PersonaId,
            CodCargo = request.CodCargo,
            Cargo = request.Cargo?.Trim(),
            ChoferPersonaId = request.ChoferPersonaId,
            RangoChofer = rangos[request.ChoferPersonaId],
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
