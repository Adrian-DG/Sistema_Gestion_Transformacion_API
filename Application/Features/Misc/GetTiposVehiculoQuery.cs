using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Common;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetTiposVehiculoQuery : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetTiposVehiculoQueryHandler(ICatalogRepository<TipoVehiculo> repository, IMapper mapper)
    : IRequestHandler<GetTiposVehiculoQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetTiposVehiculoQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CatalogItemViewModel>>(mapper.Map<List<CatalogItemViewModel>>(data));
    }
}
