using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Application.Features.Historico.ViewModels;
using AutoMapper;
using MediatR;

namespace Application.Features.Historico;

public class GetPolizasQuery(IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<PaginationFilterQuery<PolizaViewModel>, PagedData<PolizaViewModel>>
{
    public async Task<PagedData<PolizaViewModel>> Handle(PaginationFilterQuery<PolizaViewModel> request, CancellationToken cancellationToken)
    {
        var data = await unitOfWork.PolizaRepository.Get(request, cancellationToken);
        var rows = mapper.Map<ICollection<PolizaViewModel>>(data.Rows ?? []);

        return new PagedData<PolizaViewModel>(rows, data.PageSize, data.PageNumber);
    }
}
