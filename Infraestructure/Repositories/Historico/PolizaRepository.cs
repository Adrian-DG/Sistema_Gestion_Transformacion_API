

using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Historico;
using Application.Features.Historico.Polizas;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories.Historico
{
    public class PolizaRepository(MainContext context) : IPolizaRepository
    {
        public async Task Create(Poliza poliza, CancellationToken cancellationToken)
        {
            await context.Polizas.AddAsync(poliza, cancellationToken);
        }    

        public async Task<Result<Poliza>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var poliza = await context.Polizas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return poliza is not null
                ? Result.Success(poliza)
                : Result.Failure<Poliza>(PolizaErrors.NotFound(id));
        }

        public async Task<PagedData<PolizaViewModel>> Get(PaginationFilterQuery<PolizaViewModel> filter, CancellationToken cancellationToken)
        {
            var query = context.Polizas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Param))
            {
                query = query.Where(x => x.VehiculoId.ToString().Contains(filter.Param));
            }

            var rows = await query
                .OrderByDescending(x => x.FechaExpedicion)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new PolizaViewModel
                {
                    Id = x.Id,
                    VehiculoId = x.VehiculoId,
                    FechaExpedicion = x.FechaExpedicion,
                    FechaVencimiento = x.FechaVencimiento,
                    Vehiculo = x.Vehiculo.ToString(),
                    NumeroPoliza = x.Numero,
                    Aseguradora = x.Aseguradora.Nombre
                })
                .ToListAsync(cancellationToken);

            return new PagedData<PolizaViewModel>(rows, filter.PageSize, filter.PageNumber);
        }
    }
}
