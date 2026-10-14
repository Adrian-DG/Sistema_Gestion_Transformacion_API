using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Taller;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Taller;

/// <summary>
/// Reactiva por excepción un vehículo dado de baja: vuelve a operativo y la reactivación, con su motivo
/// y quien la autorizó, queda en el historial de mantenimientos. Requiere el permiso ReactivarVehiculos.
/// </summary>
public record ReactivarVehiculoCommand(Guid VehiculoId, string Motivo) : IRequest<Result>;

public class ReactivarVehiculoCommandValidator : AbstractValidator<ReactivarVehiculoCommand>
{
    public ReactivarVehiculoCommandValidator() => RuleFor(x => x.Motivo).NotEmpty().MaximumLength(1000);
}

public class ReactivarVehiculoCommandHandler(IUnitOfWork uow) : IRequestHandler<ReactivarVehiculoCommand, Result>
{
    public async Task<Result> Handle(ReactivarVehiculoCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = await uow.Tracked<Domain.Entities.Recursos.Vehiculo>()
            .FirstOrDefaultAsync(v => v.Id == request.VehiculoId, cancellationToken);
        if (vehiculo is null) return Result.Failure(VehiculoErrors.NotFound(request.VehiculoId));
        if (vehiculo.Estado != EstadoVehiculo.Inactivo) return Result.Failure(TallerErrors.VehiculoNoInactivo(vehiculo.Placa));

        vehiculo.Estado = EstadoVehiculo.Operativo;
        await uow.AddAsync(new ReactivacionVehiculo
        {
            VehiculoId = vehiculo.Id,
            Fecha = DateTime.Now,
            Motivo = request.Motivo.Trim(),
            Status = true
        }, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
