using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetTipoDocumentosQuery : IRequest<IReadOnlyList<CatalogItemViewModel>>;

public class GetTipoDocumentosQueryHandler(ICatalogRepository<TipoDocumento> repository, IMapper mapper)
    : IRequestHandler<GetTipoDocumentosQuery, IReadOnlyList<CatalogItemViewModel>>
{
    public async Task<IReadOnlyList<CatalogItemViewModel>> Handle(GetTipoDocumentosQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<CatalogItemViewModel>>(data);
    }
}
