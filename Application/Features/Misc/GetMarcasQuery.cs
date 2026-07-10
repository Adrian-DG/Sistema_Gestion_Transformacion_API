using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetMarcasQuery : IRequest<IReadOnlyList<CatalogItemViewModel>>;

public class GetMarcasQueryHandler(ICatalogRepository<Marca> repository, IMapper mapper)
    : IRequestHandler<GetMarcasQuery, IReadOnlyList<CatalogItemViewModel>>
{
    public async Task<IReadOnlyList<CatalogItemViewModel>> Handle(GetMarcasQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<CatalogItemViewModel>>(data);
    }
}
