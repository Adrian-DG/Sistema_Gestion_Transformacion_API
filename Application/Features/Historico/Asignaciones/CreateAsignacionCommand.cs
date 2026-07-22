using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var vehiculo = await unitOfWork.Query<Domain.Entities.Recursos.Vehiculo>()
            .FirstAsync(v => v.Id == request.VehiculoId, cancellationToken);

        if (vehiculo is null)
        {
            return Result.Failure<Guid>(VehiculoErrors.NotFound(request.VehiculoId));
        }        

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
            Estatus = EstatusAsignacion.PendienteConfirmacion
        };

        vehiculo.Disponible = false;

        await unitOfWork.AsignacionRepository.Create(asignacion, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(asignacion.Id);
    }
}
