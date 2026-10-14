using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Taller.Solicitudes;

/// <summary>Kilometraje y garantía del vehículo, para decidir dónde se le da mantenimiento.</summary>
public class GarantiaVehiculoViewModel
{
    public Guid VehiculoId { get; set; }
    public string Placa { get; set; } = string.Empty;
    /// <summary>Último kilometraje registrado; el de una solicitud nueva no puede ser menor.</summary>
    public int Kilometraje { get; set; }
    public bool EnGarantia { get; set; }
    public DateOnly? GarantiaVence { get; set; }
    public int? GarantiaKilometraje { get; set; }
    /// <summary>Taller autorizado (concesionario) donde se hacen los mantenimientos para conservar la garantía.</summary>
    public Guid? GarantiaTallerId { get; set; }
    public string? GarantiaTaller { get; set; }
}

/// <summary>Lo consulta el personal del taller al registrar una solicitud (no requiere el permiso de vehículos).</summary>
public record GetGarantiaVehiculoQuery(Guid VehiculoId) : IRequest<Result<GarantiaVehiculoViewModel>>;

public class GetGarantiaVehiculoQueryHandler(IUnitOfWork uow) : IRequestHandler<GetGarantiaVehiculoQuery, Result<GarantiaVehiculoViewModel>>
{
    public async Task<Result<GarantiaVehiculoViewModel>> Handle(GetGarantiaVehiculoQuery request, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var garantia = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .Where(v => v.Id == request.VehiculoId)
            .Select(v => new GarantiaVehiculoViewModel
            {
                VehiculoId = v.Id,
                Placa = v.Placa,
                Kilometraje = v.Kilometraje,
                EnGarantia = v.GarantiaVence >= hoy && (v.GarantiaKilometraje == null || v.Kilometraje <= v.GarantiaKilometraje),
                GarantiaVence = v.GarantiaVence,
                GarantiaKilometraje = v.GarantiaKilometraje,
                GarantiaTallerId = v.GarantiaTallerId,
                GarantiaTaller = v.GarantiaTaller != null ? v.GarantiaTaller.Nombre : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return garantia is null
            ? Result.Failure<GarantiaVehiculoViewModel>(VehiculoErrors.NotFound(request.VehiculoId))
            : Result.Success(garantia);
    }
}
