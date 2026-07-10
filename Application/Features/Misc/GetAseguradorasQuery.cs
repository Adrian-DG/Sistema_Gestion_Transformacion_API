using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetAseguradorasQuery : IRequest<IReadOnlyList<CatalogItemViewModel>>;

public class GetAseguradorasQueryHandler(ICatalogRepository<Aseguradora> repository, IMapper mapper)
    : IRequestHandler<GetAseguradorasQuery, IReadOnlyList<CatalogItemViewModel>>
{
    public async Task<IReadOnlyList<CatalogItemViewModel>> Handle(GetAseguradorasQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<CatalogItemViewModel>>(data);
    }
}
