using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Common;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetModelosQuery : IRequest<Result<IReadOnlyList<ModeloViewModel>>>;

public class GetModelosQueryHandler(ICatalogRepository<Modelo> repository, IMapper mapper)
    : IRequestHandler<GetModelosQuery, Result<IReadOnlyList<ModeloViewModel>>>
{
    public async Task<Result<IReadOnlyList<ModeloViewModel>>> Handle(GetModelosQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ModeloViewModel>>(mapper.Map<List<ModeloViewModel>>(data));
    }
}
