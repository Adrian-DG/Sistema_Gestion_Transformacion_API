using Application.Common.DTO;
using Application.Common.Response;
using Application.Features.Historico.Asignaciones;
using Domain.Common;
using Domain.Entities.Historico;

namespace Application.Contracts.Historico
{
    public interface IAsignacionRepository
    {
        Task Create(Asignacion asignacion, CancellationToken cancellationToken);
        Task<PagedData<AsignacionViewModel>> Get(PaginationFilterQuery<AsignacionViewModel> filter, CancellationToken cancellationToken);
        Task<Result<Asignacion>> GetById(Guid id, CancellationToken cancellationToken);
    }
}
