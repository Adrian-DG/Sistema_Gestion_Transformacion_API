
using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using MediatR;

namespace Application.Features.Historico.Asignaciones;

public class AsignacionViewModel
{
    public Guid Id { get; set; }
    public Guid PersonaId { get; set; }
    public Guid VehiculoId { get; set; }
    public DateOnly FechaEfectividad { get; set; }
    public string? Motivo { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public int CantidadAdjuntos { get; set; }
}

public class GetAsignacionesQuery(IUnitOfWork unitOfWork) : IRequestHandler<PaginationFilterQuery<AsignacionViewModel>, Result<PagedData<AsignacionViewModel>>>
{
    public async Task<Result<PagedData<AsignacionViewModel>>> Handle(PaginationFilterQuery<AsignacionViewModel> request, CancellationToken cancellationToken)
    {
        var data = await unitOfWork.AsignacionRepository.Get(request, cancellationToken);
        return Result.Success(data);
    }
}


