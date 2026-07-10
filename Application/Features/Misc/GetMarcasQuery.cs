using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Common;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetMarcasQuery : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetMarcasQueryHandler(ICatalogRepository<Marca> repository, IMapper mapper)
    : IRequestHandler<GetMarcasQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetMarcasQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CatalogItemViewModel>>(mapper.Map<List<CatalogItemViewModel>>(data));
    }
}
