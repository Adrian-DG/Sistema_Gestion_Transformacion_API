using Application.Common.DTO;
using Application.Common.Response;
using Application.Features.Historico.Polizas;
using Domain.Entities.Historico;

namespace Application.Contracts.Historico
{
    public interface IPolizaRepository
    {
        Task Create(Poliza poliza, CancellationToken cancellationToken);
        Task<PagedData<PolizaViewModel>> Get(PaginationFilterQuery<PolizaViewModel> filter, CancellationToken cancellationToken);
        Task<Poliza?> GetById(Guid id, CancellationToken cancellationToken);
    }
}
