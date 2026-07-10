using Application.Common.DTO;
using Application.Common.Response;
using Application.Features.Historico.ViewModels;
using Domain.Entities.Historico;

namespace Application.Contracts.Historico
{
    public interface IPolizaRepository
    {
        Task Create(Poliza poliza, CancellationToken cancellationToken);
        Task<PagedData<Poliza>> Get(PaginationFilterQuery<PolizaViewModel> filter, CancellationToken cancellationToken);
        Task<Poliza?> GetById(Guid id, CancellationToken cancellationToken);
    }
}
