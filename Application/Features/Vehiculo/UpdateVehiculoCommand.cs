using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Vehiculo;

/// <summary>
/// Actualiza los datos del vehículo. Los vehículos no se eliminan: <paramref name="Disponible"/> en false
/// lo deja fuera de servicio (no se puede asignar) y conserva su historial.
/// </summary>
public record UpdateVehiculoCommand(
    Guid Id,
    string Chasis,
    string Matricula,
    string Placa,
    int Fabricacion,
    Guid MarcaId,
    Guid ModeloId,
    Guid TipoId,
    Guid ColorId,
    bool Disponible
) : IRequest<Result>, IDatosVehiculo;

public class UpdateVehiculoCommandValidator : DatosVehiculoValidator<UpdateVehiculoCommand>
{
    public UpdateVehiculoCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class UpdateVehiculoCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateVehiculoCommand, Result>
{
    public async Task<Result> Handle(UpdateVehiculoCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = await uow.Tracked<Domain.Entities.Recursos.Vehiculo>()
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (vehiculo is null) return Result.Failure(VehiculoErrors.NotFound(request.Id));

        var validacion = await VehiculoReglas.ValidarAsync(uow, request, request.Id, cancellationToken);
        if (validacion.IsFailure) return validacion;

        VehiculoReglas.Aplicar(vehiculo, request);
        vehiculo.Disponible = request.Disponible;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
