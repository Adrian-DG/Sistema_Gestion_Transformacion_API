using Application.Contracts;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Historico;

public record CreateAdjuntoRequest(Guid TipoDocumentoId, Guid TipoOperacionId, string FileBase64);

public record CreateAsignacionCommand(
    Guid PersonaId,
    Guid VehiculoId,
    DateOnly FechaEfectividad,
    string Motivo,
    List<CreateAdjuntoRequest> Adjuntos
) : IRequest<Guid>;

public class CreateAsignacionCommandValidator : AbstractValidator<CreateAsignacionCommand>
{
    public CreateAsignacionCommandValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty();
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Adjuntos).NotNull().Must(x => x.Count > 0)
            .WithMessage("Debe adjuntar al menos un documento para registrar la asignación.");
    }
}

public class CreateAsignacionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateAsignacionCommand, Guid>
{
    public async Task<Guid> Handle(CreateAsignacionCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = await unitOfWork.VehiculoRepository.GetById(request.VehiculoId, cancellationToken);

        if (!vehiculo.Disponible)
        {
            throw new InvalidOperationException("El vehículo seleccionado no está disponible para asignación.");
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
