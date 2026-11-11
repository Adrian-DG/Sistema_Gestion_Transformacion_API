using Application.Contracts;
using Application.Contracts.Reportes;
using Application.Contracts.Usuarios;
using Application.Features.Configuracion;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Configuracion;
using Domain.Entities.Historico;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reportes.Asignaciones;

/// <summary>
/// PDF de una asignación de vehículo con el encabezado de la plantilla de reportes predeterminada.
/// Logo: el de la plantilla; si no tiene, el de la apariencia activa; si tampoco, el predeterminado
/// del sistema. Solo las asignaciones aprobadas o finalizadas son una constancia válida; las demás
/// se marcan como tales.
/// </summary>
public record GetConstanciaAsignacionQuery(Guid AsignacionId) : IRequest<Result<ArchivoReporte>>;

public class GetConstanciaAsignacionQueryHandler(
    IUnitOfWork uow, IMediator mediator, IUsuarioService usuarios, IPdfReportGenerator pdf)
    : IRequestHandler<GetConstanciaAsignacionQuery, Result<ArchivoReporte>>
{
    public async Task<Result<ArchivoReporte>> Handle(GetConstanciaAsignacionQuery request, CancellationToken cancellationToken)
    {
        var a = await uow.Query<Asignacion>()
            .Where(x => x.Id == request.AsignacionId)
            .Select(x => new
            {
                x.Id,
                x.Estatus,
                x.CreatedAt,
                x.CreatedBy,
                x.FechaAprobacion,
                x.AprobadoPor,
                Solicitante = new PersonaConstancia(
                    x.SolicitantePersona!.Nombre + " " + x.SolicitantePersona.Apellido,
                    x.SolicitantePersona.Identificacion,
                    x.RangoSolicitante),
                x.CargoSolicitante,
                x.DependenciaSolicitante,
                Asignado = new PersonaConstancia(
                    x.Persona!.Nombre + " " + x.Persona.Apellido,
                    x.Persona.Identificacion,
                    x.Persona.Rango != null ? x.Persona.Rango.Nombre : null),
                x.Cargo,
                Chofer = new PersonaConstancia(
                    x.ChoferPersona!.Nombre + " " + x.ChoferPersona.Apellido,
                    x.ChoferPersona.Identificacion,
                    x.RangoChofer),
                Vehiculo = new VehiculoConstancia(
                    x.Vehiculo!.Placa,
                    x.Vehiculo.Ficha,
                    x.Vehiculo.Marca!.Nombre,
                    x.Vehiculo.Modelo!.Nombre,
                    x.Vehiculo.Fabricacion,
                    x.Vehiculo.Color!.Nombre,
                    x.Vehiculo.Tipo!.Nombre,
                    x.Vehiculo.Chasis,
                    x.Vehiculo.Matricula,
                    x.Vehiculo.Kilometraje),
                x.FechaSalida,
                x.FechaRetornoEstimada,
                x.FechaRetornoReal,
                x.Proposito,
                x.Destino,
                x.MotivoRechazoCancelacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (a is null) return Result.Failure<ArchivoReporte>(AsignacionErrors.NotFound(request.AsignacionId));

        var ids = new List<Guid> { a.CreatedBy };
        if (a.AprobadoPor is { } aprobador) ids.Add(aprobador);
        var nombres = await usuarios.GetNombresAsync(ids, cancellationToken);

        var datos = new ConstanciaAsignacion(
            a.Id,
            NombreEstatus(a.Estatus),
            a.Estatus is EstatusSolicitud.Aprobada or EstatusSolicitud.Finalizada,
            a.CreatedAt,
            nombres.GetValueOrDefault(a.CreatedBy) ?? "—",
            a.FechaAprobacion,
            a.AprobadoPor is { } id ? nombres.GetValueOrDefault(id) : null,
            a.Solicitante,
            a.CargoSolicitante,
            a.DependenciaSolicitante,
            a.Asignado,
            a.Cargo,
            a.Chofer,
            a.Vehiculo,
            a.FechaSalida,
            a.FechaRetornoEstimada,
            a.FechaRetornoReal,
            a.Proposito,
            a.Destino,
            a.MotivoRechazoCancelacion,
            DateTime.Now);

        var encabezado = await EncabezadoPdf.ObtenerAsync(mediator, uow, TipoReportePdf.AsignacionVehiculo, cancellationToken);
        var contenido = pdf.ConstanciaAsignacion(encabezado, datos);
        return Result.Success(new ArchivoReporte(contenido, $"Asignacion_{a.Vehiculo.Placa}_{datos.Referencia}.pdf"));
    }

    private static string NombreEstatus(EstatusSolicitud estatus) => estatus switch
    {
        EstatusSolicitud.Pendiente => "Pendiente de aprobación",
        EstatusSolicitud.Aprobada => "Aprobada",
        EstatusSolicitud.Rechazada => "Rechazada",
        EstatusSolicitud.Cancelada => "Cancelada",
        EstatusSolicitud.Finalizada => "Finalizada",
        _ => estatus.ToString()
    };
}
