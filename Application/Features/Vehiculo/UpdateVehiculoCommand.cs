using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Vehiculo;

/// <summary>
/// Actualiza los datos del vehículo. Su estado (operativo, en mantenimiento o inactivo) no se edita
/// aquí: lo cambia el taller. La condición y el kilometraje de ingreso no cambian, y el kilometraje
/// no puede bajar. Los vehículos no se eliminan; la baja los deja inactivos con su historial.
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
    string? Ficha = null,
    int? Kilometraje = null,
    bool? TieneGarantia = null,
    DateOnly? GarantiaVence = null,
    int? GarantiaKilometraje = null,
    Guid? GarantiaTallerId = null
): IRequest<Result>, IDatosVehiculo;

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

        if (request.Kilometraje < vehiculo.Kilometraje) return Result.Failure(VehiculoErrors.KilometrajeMenor(vehiculo.Kilometraje));

        var validacion = await VehiculoReglas.ValidarAsync(uow, request, request.Id, cancellationToken);
        if (validacion.IsFailure) return validacion;

        VehiculoReglas.Aplicar(vehiculo, request);

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
