using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Historico.Asignaciones;

public class AsignacionViewModel
{
    public Guid Id { get; set; }
    public string Estatus { get; set; } = string.Empty;

    public string Solicitante { get; set; } = string.Empty;
    public string SolicitanteCedula { get; set; } = string.Empty;
    public string? RangoSolicitante { get; set; }
    public string CargoSolicitante { get; set; } = string.Empty;
    public string DependenciaSolicitante { get; set; } = string.Empty;

    public string TipoAsignacion { get; set; } = string.Empty;
    /// <summary>Persona a quien se asigna el vehículo (en asignaciones al cargo, quien lo recibe).</summary>
    public string AsignadoA { get; set; } = string.Empty;
    public string AsignadoACedula { get; set; } = string.Empty;
    /// <summary>CodCargo de SIPFFAA, solo en asignaciones al cargo.</summary>
    public int? CodCargo { get; set; }
    public string? Cargo { get; set; }

    public string Chofer { get; set; } = string.Empty;
    public string ChoferCedula { get; set; } = string.Empty;
    public string? RangoChofer { get; set; }

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

/// <summary>
/// Listado paginado; <see cref="Param"/> busca por texto, <see cref="Estatus"/> filtra por estado y
/// <see cref="VehiculoId"/> por vehículo (historial de asignaciones del vehículo).
/// </summary>
public record FiltroAsignacionesQuery : PaginationFilterQuery<AsignacionViewModel>
{
    public EstatusSolicitud? Estatus { get; set; }
    public Guid? VehiculoId { get; set; }
}

public class FiltroAsignacionesQueryValidator : AbstractValidator<FiltroAsignacionesQuery>
{
    public FiltroAsignacionesQueryValidator()
    {
        Include(new PaginationFilterQueryValidator<AsignacionViewModel>());
        RuleFor(x => x.Estatus).IsInEnum();
    }
}

public class GetAsignacionesQuery(IUnitOfWork unitOfWork) : IRequestHandler<FiltroAsignacionesQuery, Result<PagedData<AsignacionViewModel>>>
{
    public async Task<Result<PagedData<AsignacionViewModel>>> Handle(FiltroAsignacionesQuery request, CancellationToken cancellationToken)
    {
        var data = await unitOfWork.AsignacionRepository.Get(request, cancellationToken);
        return Result.Success(data);
    }
}
