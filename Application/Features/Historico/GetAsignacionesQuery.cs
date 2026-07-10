using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Application.Features.Historico.ViewModels;
using MediatR;

namespace Application.Features.Historico;

public class GetAsignacionesQuery(IUnitOfWork unitOfWork) : IRequestHandler<PaginationFilterQuery<AsignacionViewModel>, PagedData<AsignacionViewModel>>
{
    public async Task<PagedData<AsignacionViewModel>> Handle(PaginationFilterQuery<AsignacionViewModel> request, CancellationToken cancellationToken)
    {
        return await unitOfWork.AsignacionRepository.Get(request, cancellationToken);
    }
}
