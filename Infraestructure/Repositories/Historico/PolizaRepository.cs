using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Historico;
using Application.Features.Historico.ViewModels;
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

        public async Task<PagedData<Poliza>> Get(PaginationFilterQuery<PolizaViewModel> filter, CancellationToken cancellationToken)
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
                .ToListAsync(cancellationToken);

            return new PagedData<Poliza>(rows, filter.PageSize, filter.PageNumber);
        }

        public async Task<Poliza?> GetById(Guid id, CancellationToken cancellationToken)
        {
            return await context.Polizas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
    }
}
