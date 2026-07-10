using Application.Contracts.Misc;
using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Common;
using Domain.Entities.Misc;
using MediatR;

namespace Application.Features.Misc;

public record GetRangosQuery : IRequest<Result<IReadOnlyList<RangoViewModel>>>;

public class GetRangosQueryHandler(ICatalogRepository<Rango> repository, IMapper mapper)
    : IRequestHandler<GetRangosQuery, Result<IReadOnlyList<RangoViewModel>>>
{
    public async Task<Result<IReadOnlyList<RangoViewModel>>> Handle(GetRangosQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RangoViewModel>>(mapper.Map<List<RangoViewModel>>(data));
    }
}
