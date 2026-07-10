using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetRangosQuery : IRequest<IReadOnlyList<RangoViewModel>>;

public class GetRangosQueryHandler(ICatalogRepository<Rango> repository, IMapper mapper)
    : IRequestHandler<GetRangosQuery, IReadOnlyList<RangoViewModel>>
{
    public async Task<IReadOnlyList<RangoViewModel>> Handle(GetRangosQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<RangoViewModel>>(data);
    }
}
