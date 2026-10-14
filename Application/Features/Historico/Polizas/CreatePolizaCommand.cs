using Application.Contracts;
using Domain.Common;
using Domain.Entities.Historico;
using MediatR;

namespace Application.Features.Historico.Polizas;

public record CreatePolizaCommand(
    Guid VehiculoId,
    Guid AseguradoraId,
    string Numero,
    DateOnly FechaExpedicion,
    DateOnly FechaEfectividad,
    DateOnly FechaVencimiento
) : IRequest<Result<Guid>>, IDatosPoliza;

public class CreatePolizaCommandValidator : DatosPolizaValidator<CreatePolizaCommand>;

public class CreatePolizaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreatePolizaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePolizaCommand request, CancellationToken cancellationToken)
    {
        var validacion = await PolizaReglas.ValidarAsync(unitOfWork, request, null, cancellationToken);
        if (validacion.IsFailure) return Result.Failure<Guid>(validacion.Error);

        var poliza = new Poliza { Numero = request.Numero, Status = true };
        PolizaReglas.Aplicar(poliza, request);

        await unitOfWork.PolizaRepository.Create(poliza, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return poliza.Id;
    }
}
