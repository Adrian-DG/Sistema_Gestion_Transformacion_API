using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetModelosQuery : IRequest<IReadOnlyList<ModeloViewModel>>;

public class GetModelosQueryHandler(ICatalogRepository<Modelo> repository, IMapper mapper)
    : IRequestHandler<GetModelosQuery, IReadOnlyList<ModeloViewModel>>
{
    public async Task<IReadOnlyList<ModeloViewModel>> Handle(GetModelosQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<ModeloViewModel>>(data);
    }
}
