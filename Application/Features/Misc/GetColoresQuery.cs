using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetColoresQuery : IRequest<IReadOnlyList<CatalogItemViewModel>>;

public class GetColoresQueryHandler(ICatalogRepository<Color> repository, IMapper mapper)
    : IRequestHandler<GetColoresQuery, IReadOnlyList<CatalogItemViewModel>>
{
    public async Task<IReadOnlyList<CatalogItemViewModel>> Handle(GetColoresQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<CatalogItemViewModel>>(data);
    }
}
