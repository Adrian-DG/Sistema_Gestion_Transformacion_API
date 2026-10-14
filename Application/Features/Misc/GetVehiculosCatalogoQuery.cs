using Application.Contracts;
using Application.Features.Misc.ViewModels;
using Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Misc;

/// <summary>
/// Vehículos para listas de selección (ej. al registrar una póliza), sin requerir el permiso de
/// gestión de vehículos. Nombre: "Placa · Marca Modelo (año)".
/// </summary>
public record GetVehiculosCatalogoQuery : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetVehiculosCatalogoQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetVehiculosCatalogoQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetVehiculosCatalogoQuery request, CancellationToken cancellationToken)
    {
        var vehiculos = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
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
