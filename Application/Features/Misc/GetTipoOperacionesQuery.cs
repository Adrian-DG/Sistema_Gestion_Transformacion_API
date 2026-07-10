using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetTipoOperacionesQuery : IRequest<IReadOnlyList<CatalogItemViewModel>>;

public class GetTipoOperacionesQueryHandler(ICatalogRepository<TipoOperacion> repository, IMapper mapper)
    : IRequestHandler<GetTipoOperacionesQuery, IReadOnlyList<CatalogItemViewModel>>
{
    public async Task<IReadOnlyList<CatalogItemViewModel>> Handle(GetTipoOperacionesQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<CatalogItemViewModel>>(data);
    }
}
