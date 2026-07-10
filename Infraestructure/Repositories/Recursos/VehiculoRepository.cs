using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Recursos;
using Application.Features.Vehiculo;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Recursos;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories.Recursos
{
    public class VehiculoRepository(MainContext context) : IVehiculoRepository
    {
        public async Task Create(Vehiculo vehiculo, CancellationToken cancellationToken)
        {
            await context.Vehiculos.AddAsync(vehiculo, cancellationToken);
        }

        public async Task<PagedData<VehiculoViewModel>> Get(PaginationFilterQuery<VehiculoViewModel> filter, CancellationToken cancellationToken)
        {
            var query = context.Vehiculos.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Param))
            {
                query = query.Where(v => v.Chasis.Contains(filter.Param) || v.Placa.Contains(filter.Param));
            }

            var result = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(v => new VehiculoViewModel
                {
                    Id = v.Id,
                    Chasis = v.Chasis,
                    Placa = v.Placa,
                    Marca = v.Marca!.Nombre,
                    Modelo = v.Modelo!.Nombre,
                    Fabricacion = v.Fabricacion,
                    Color = v.Color!.Nombre
                }).ToListAsync(cancellationToken);

            return new PagedData<VehiculoViewModel>(result, filter.PageSize, filter.PageNumber);
        }

        public async Task<Result<Vehiculo>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var vehiculo = await context.Vehiculos.FindAsync([id], cancellationToken);

            return vehiculo is not null
                ? Result.Success(vehiculo)
                : Result.Failure<Vehiculo>(VehiculoErrors.NotFound(id));
        }
    }
}
