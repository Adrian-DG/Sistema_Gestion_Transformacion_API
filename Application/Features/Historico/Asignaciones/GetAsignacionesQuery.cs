using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using MediatR;

namespace Application.Features.Historico.Asignaciones;

public class AsignacionViewModel
{
    public Guid Id { get; set; }
    public string Estatus { get; set; } = string.Empty;

    public string TipoAsignacion { get; set; } = string.Empty;
    public Guid? CargoId { get; set; }
    public string? Cargo { get; set; }
    /// <summary>Persona asignada, o titular actual cuando la asignación es al cargo.</summary>
    public string? Responsable { get; set; }

    public string Solicitante { get; set; } = string.Empty;
    public string DependenciaSolicitante { get; set; } = string.Empty;
    public string Proposito { get; set; } = string.Empty;
    public string? Destino { get; set; }

    public Guid VehiculoId { get; set; }
    public string Chasis { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;

    public DateTime FechaSalida { get; set; }
    public DateTime FechaRetornoEstimada { get; set; }
    public DateTime? FechaRetornoReal { get; set; }
    public string? MotivoRechazoCancelacion { get; set; }
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
