using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Recursos;
using Application.Features.Vehiculo;
using Domain.Entities.Recursos;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Infraestructure.Repositories.Recursos
{
    public class VehiculoRepository(MainContext context) : IVehiculoRepository
    {
        public async Task Create(Vehiculo vehiculo, CancellationToken cancellationToken)
        {
            await context.Vehiculos.AddAsync(vehiculo);
            await context.SaveChangesAsync();
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
                    Marca = v.Marca.Nombre,
                    Modelo = v.Modelo.Nombre,
                    Fabricacion = v.Fabricacion,
                    Color = v.Color.Nombre
                }).ToListAsync();
               

            return new PagedData<VehiculoViewModel>(result, filter.PageSize, filter.PageNumber);

        }
    }
}
