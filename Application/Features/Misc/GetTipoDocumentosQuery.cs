using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Common;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetTipoDocumentosQuery : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetTipoDocumentosQueryHandler(ICatalogRepository<TipoDocumento> repository, IMapper mapper)
    : IRequestHandler<GetTipoDocumentosQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetTipoDocumentosQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CatalogItemViewModel>>(mapper.Map<List<CatalogItemViewModel>>(data));
    }
}
