using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Historico;
using Application.Features.Historico.Asignaciones;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories.Historico
{
    public class AsignacionRepository(MainContext context) : IAsignacionRepository
    {
        public async Task Create(Asignacion asignacion, CancellationToken cancellationToken)
        {
            await context.Asignaciones.AddAsync(asignacion, cancellationToken);
        }

        public async Task<PagedData<AsignacionViewModel>> Get(PaginationFilterQuery<AsignacionViewModel> filter, CancellationToken cancellationToken)
        {
            var query = context.Asignaciones.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Param))
            {
                query = query.Where(x => (x.Motivo ?? string.Empty).Contains(filter.Param));
            }

            var totalRecords = await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderByDescending(x => x.FechaEfectividad)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new AsignacionViewModel
                {
                    Id = x.Id,
                    PersonaId = x.PersonaId,
                    VehiculoId = x.VehiculoId,
                    FechaEfectividad = x.FechaEfectividad,
                    Motivo = x.Motivo,
                    Estatus = x.Estatus.ToString(),
                    CantidadAdjuntos = x.Adjuntos != null ? x.Adjuntos.Count : 0
                })
                .ToListAsync(cancellationToken);

            return new PagedData<AsignacionViewModel>(rows, filter.PageSize, filter.PageNumber, totalRecords);
        }

        public async Task<Result<Asignacion>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var asignacion = await context.Asignaciones
                .Include(x => x.Adjuntos)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return asignacion is not null
                ? Result.Success(asignacion)
                : Result.Failure<Asignacion>(AsignacionErrors.NotFound(id));
        }
    }
}
