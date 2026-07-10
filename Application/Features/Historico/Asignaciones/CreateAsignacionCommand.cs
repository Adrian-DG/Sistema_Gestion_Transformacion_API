using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Historico.Asignaciones;

public record CreateAdjuntoRequest(Guid TipoDocumentoId, Guid TipoOperacionId, string FileBase64);

public record CreateAsignacionCommand(
    Guid PersonaId,
    Guid VehiculoId,
    DateOnly FechaEfectividad,
    string Motivo,
    List<CreateAdjuntoRequest> Adjuntos
) : IRequest<Result<Guid>>;

public class CreateAsignacionCommandValidator : AbstractValidator<CreateAsignacionCommand>
{
    public CreateAsignacionCommandValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty();
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Adjuntos)
            .NotNull()
            .NotEmpty()
            .WithMessage("Debe adjuntar al menos un documento para registrar la asignación.");
    }
}

public class CreateAsignacionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateAsignacionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAsignacionCommand request, CancellationToken cancellationToken)
    {
        var vehiculoResult = await unitOfWork.VehiculoRepository.GetById(request.VehiculoId, cancellationToken);

        if (vehiculoResult.IsFailure)
        {
            return Result.Failure<Guid>(vehiculoResult.Error);
        }

        var vehiculo = vehiculoResult.Value;

        if (!vehiculo.Disponible)
        {
            return Result.Failure<Guid>(VehiculoErrors.NotAvailable(vehiculo.Id));
        }

        var asignacion = new Asignacion
        {
            PersonaId = request.PersonaId,
            VehiculoId = request.VehiculoId,
            FechaEfectividad = request.FechaEfectividad,
            Motivo = request.Motivo,
            Estatus = EstatusAsignacion.PendienteConfirmacion,
            Adjuntos = request.Adjuntos.Select(x => new Adjunto
            {
                TipoDocumentoId = x.TipoDocumentoId,
                TipoOperacionId = x.TipoOperacionId,
                File = Convert.FromBase64String(x.FileBase64),
            }).ToList()
        };

        vehiculo.Disponible = false;

        await unitOfWork.AsignacionRepository.Create(asignacion, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return asignacion.Id;
    }
}
