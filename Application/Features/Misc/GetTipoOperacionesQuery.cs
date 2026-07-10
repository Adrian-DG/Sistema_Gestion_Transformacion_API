using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Common;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetTipoOperacionesQuery : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetTipoOperacionesQueryHandler(ICatalogRepository<TipoOperacion> repository, IMapper mapper)
    : IRequestHandler<GetTipoOperacionesQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetTipoOperacionesQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CatalogItemViewModel>>(mapper.Map<List<CatalogItemViewModel>>(data));
    }
}
