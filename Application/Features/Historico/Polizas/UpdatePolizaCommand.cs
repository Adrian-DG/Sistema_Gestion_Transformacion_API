using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico.Polizas;

/// <summary>Corrige los datos de una póliza. Para renovarla se registra una póliza nueva.</summary>
public record UpdatePolizaCommand(
    Guid Id,
    Guid VehiculoId,
    Guid AseguradoraId,
    string Numero,
    DateOnly FechaExpedicion,
    DateOnly FechaEfectividad,
    DateOnly FechaVencimiento
) : IRequest<Result>, IDatosPoliza;

public class UpdatePolizaCommandValidator : DatosPolizaValidator<UpdatePolizaCommand>
{
    public UpdatePolizaCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class UpdatePolizaCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdatePolizaCommand, Result>
{
    public async Task<Result> Handle(UpdatePolizaCommand request, CancellationToken cancellationToken)
    {
        var poliza = await uow.Tracked<Poliza>().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (poliza is null) return Result.Failure(PolizaErrors.NotFound(request.Id));

        var validacion = await PolizaReglas.ValidarAsync(uow, request, request.Id, cancellationToken);
        if (validacion.IsFailure) return validacion;

        PolizaReglas.Aplicar(poliza, request);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
