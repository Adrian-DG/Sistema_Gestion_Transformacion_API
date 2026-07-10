using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetTiposVehiculoQuery : IRequest<IReadOnlyList<CatalogItemViewModel>>;

public class GetTiposVehiculoQueryHandler(ICatalogRepository<TipoVehiculo> repository, IMapper mapper)
    : IRequestHandler<GetTiposVehiculoQuery, IReadOnlyList<CatalogItemViewModel>>
{
    public async Task<IReadOnlyList<CatalogItemViewModel>> Handle(GetTiposVehiculoQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<CatalogItemViewModel>>(data);
    }
}
