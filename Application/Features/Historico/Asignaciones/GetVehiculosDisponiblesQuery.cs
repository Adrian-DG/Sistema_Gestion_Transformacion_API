using Application.Contracts;
using Application.Features.Misc.ViewModels;
using Domain.Common;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico.Asignaciones;

/// <summary>
/// Vehículos en servicio sin asignaciones aprobadas que choquen con el período (misma regla que
/// <see cref="DisponibilidadVehiculo.VehiculoOcupadoAsync"/>). Nombre: "Placa · Marca Modelo (año)".
/// </summary>
public record GetVehiculosDisponiblesQuery(DateTime Salida, DateTime RetornoEstimado)
    : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetVehiculosDisponiblesQueryValidator : AbstractValidator<GetVehiculosDisponiblesQuery>
{
    public GetVehiculosDisponiblesQueryValidator() =>
        RuleFor(x => x.RetornoEstimado)
            .GreaterThan(x => x.Salida)
            .WithMessage("La fecha de retorno estimada debe ser posterior a la fecha de salida.");
}

public class GetVehiculosDisponiblesQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetVehiculosDisponiblesQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetVehiculosDisponiblesQuery request, CancellationToken cancellationToken)
    {
        var ahora = DateTime.Now;
        var ocupados = uow.Query<Asignacion>()
            .Where(a =>
                a.Estatus == EstatusSolicitud.Aprobada &&
                a.FechaSalida.AddHours(-Asignacion.HorasReservaPrevia) < request.RetornoEstimado &&
                (a.FechaRetornoEstimada > request.Salida || a.FechaRetornoEstimada < ahora))
            .Select(a => a.VehiculoId);

        var vehiculos = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .Where(v => v.Disponible && !ocupados.Contains(v.Id))
            .OrderBy(v => v.Placa)
            .Select(v => new CatalogItemViewModel
            {
                Id = v.Id,
                Nombre = v.Placa + " · " + v.Marca!.Nombre + " " + v.Modelo!.Nombre + " (" + v.Fabricacion + ")"
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CatalogItemViewModel>>(vehiculos);
    }
}
