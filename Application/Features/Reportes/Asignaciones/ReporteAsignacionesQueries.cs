using Application.Contracts;
using Application.Contracts.Reportes;
using Domain.Common;
using Domain.Entities.Combustible;
using Domain.Entities.Historico;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reportes.Asignaciones;

/// <summary>Las asignaciones se incluyen según su fecha de salida.</summary>
public record GetEstadisticasAsignacionesQuery(TipoPeriodo Periodo, DateOnly? Fecha, DateOnly? Desde, DateOnly? Hasta)
    : IRequest<Result<EstadisticasAsignacionesViewModel>>, IPeriodoRequest;

public record GetReporteExcelAsignacionesQuery(TipoPeriodo Periodo, DateOnly? Fecha, DateOnly? Desde, DateOnly? Hasta)
    : IRequest<Result<ArchivoReporte>>, IPeriodoRequest;

public class GetEstadisticasAsignacionesQueryValidator : PeriodoRequestValidator<GetEstadisticasAsignacionesQuery>;

public class GetReporteExcelAsignacionesQueryValidator : PeriodoRequestValidator<GetReporteExcelAsignacionesQuery>;

public class EstadisticasAsignacionesViewModel
{
    public required PeriodoViewModel Periodo { get; set; }
    public int Total { get; set; }

    /// <summary>Promedio de días de uso de las asignaciones finalizadas.</summary>
    public double? DuracionPromedioDias { get; set; }

    /// <summary>Asignaciones finalizadas cuyo vehículo regresó después del retorno estimado.</summary>
    public int RetornosTardios { get; set; }

    /// <summary>Asignaciones aprobadas cuyo retorno estimado ya pasó sin registrar la entrada del vehículo.</summary>
    public int VencidasSinRetorno { get; set; }

    public decimal GalonesDespachados { get; set; }

    public List<ConteoViewModel> PorEstatus { get; set; } = [];
    public List<ConteoViewModel> PorTipoAsignacion { get; set; } = [];
    public List<ConteoViewModel> TopDependencias { get; set; } = [];
    public List<ConteoViewModel> TopVehiculos { get; set; } = [];
    public List<SerieAsignacionesViewModel> Serie { get; set; } = [];
}

public class SerieAsignacionesViewModel
{
    public DateOnly Fecha { get; set; }
    public int Total { get; set; }
    public int Pendientes { get; set; }
    /// <summary>Aprobadas y finalizadas.</summary>
    public int Aprobadas { get; set; }
    /// <summary>Rechazadas y canceladas.</summary>
    public int NoAprobadas { get; set; }
}

public record AsignacionReporteRow(
    DateTime FechaSalida,
    DateTime FechaRetornoEstimada,
    DateTime? FechaRetornoReal,
    EstatusSolicitud Estatus,
    TipoBeneficiario TipoAsignacion,
    string Solicitante,
    string CargoSolicitante,
    string Dependencia,
    string AsignadoA,
    string? Cargo,
    string Chofer,
    string Proposito,
    string? Destino,
    string Placa,
    string Vehiculo,
    decimal GalonesDespachados,
    string? MotivoRechazoCancelacion)
{
    public bool RetornoTardio => FechaRetornoReal > FechaRetornoEstimada;
    public double? DiasDeUso => FechaRetornoReal.HasValue ? (FechaRetornoReal.Value - FechaSalida).TotalDays : null;
}

public class ReporteAsignacionesQueryHandler(IUnitOfWork uow, IExcelReportGenerator excel) :
    IRequestHandler<GetEstadisticasAsignacionesQuery, Result<EstadisticasAsignacionesViewModel>>,
    IRequestHandler<GetReporteExcelAsignacionesQuery, Result<ArchivoReporte>>
{
    private const int TopCantidad = 10;

    public async Task<Result<EstadisticasAsignacionesViewModel>> Handle(GetEstadisticasAsignacionesQuery request, CancellationToken cancellationToken)
    {
        var periodo = PeriodoReporte.Resolver(request);
        var rows = await CargarAsync(periodo, cancellationToken);
        return Result.Success(Calcular(periodo, rows));
    }

    public async Task<Result<ArchivoReporte>> Handle(GetReporteExcelAsignacionesQuery request, CancellationToken cancellationToken)
    {
        var periodo = PeriodoReporte.Resolver(request);
        var rows = await CargarAsync(periodo, cancellationToken);
        var estadisticas = Calcular(periodo, rows);

        var libro = new LibroReporte("Reporte de asignaciones de vehículos", periodo.Descripcion,
        [
            new HojaReporte("Resumen", "Indicadores del período",
                ["Indicador", "Valor"],
                [
                    ["Total de asignaciones", estadisticas.Total],
                    ["Duración promedio (días)", estadisticas.DuracionPromedioDias is { } d ? Math.Round(d, 2) : null],
                    ["Retornos tardíos", estadisticas.RetornosTardios],
                    ["Vencidas sin retorno", estadisticas.VencidasSinRetorno],
                    ["Galones despachados", estadisticas.GalonesDespachados],
                    .. estadisticas.PorEstatus.Select(e => new object?[] { $"Estatus: {e.Nombre}", e.Cantidad }),
                    .. estadisticas.PorTipoAsignacion.Select(t => new object?[] { $"Asignadas a: {t.Nombre}", t.Cantidad })
                ]),

            new HojaReporte("Por dependencia", $"Dependencias con más solicitudes (top {TopCantidad})",
                ["Dependencia", "Asignaciones", "Galones despachados"],
                estadisticas.TopDependencias.Select(c => new object?[] { c.Nombre, c.Cantidad, c.Galones }).ToList(),
                [1, 2]),

            new HojaReporte("Por vehículo", $"Vehículos más asignados (top {TopCantidad})",
                ["Vehículo", "Asignaciones", "Galones despachados"],
                estadisticas.TopVehiculos.Select(c => new object?[] { c.Nombre, c.Cantidad, c.Galones }).ToList(),
                [1, 2]),

            new HojaReporte("Serie", $"Asignaciones por {(periodo.Agrupacion == AgrupacionSerie.Mes ? "mes" : "día")} de salida",
                ["Fecha", "Total", "Pendientes", "Aprobadas", "Rechazadas / canceladas"],
                estadisticas.Serie.Select(s => new object?[] { s.Fecha, s.Total, s.Pendientes, s.Aprobadas, s.NoAprobadas }).ToList(),
                [1, 2, 3, 4]),

            new HojaReporte("Detalle", "Detalle de asignaciones",
                ["Salida", "Retorno estimado", "Retorno real", "Días de uso", "Retorno tardío", "Estatus", "Tipo",
                 "Solicitante", "Cargo solicitante", "Dependencia", "Asignado a", "Cargo asignado", "Chofer", "Placa", "Vehículo",
                 "Destino", "Propósito", "Galones despachados", "Motivo rechazo / cancelación"],
                rows.Select(r => new object?[]
                {
                    r.FechaSalida, r.FechaRetornoEstimada, r.FechaRetornoReal,
                    r.DiasDeUso is { } dias ? Math.Round(dias, 2) : null, r.RetornoTardio,
                    r.Estatus.ToString(), r.TipoAsignacion.ToString(),
                    r.Solicitante, r.CargoSolicitante, r.Dependencia, r.AsignadoA, r.Cargo, r.Chofer,
                    r.Placa, r.Vehiculo, r.Destino, r.Proposito,
                    r.GalonesDespachados, r.MotivoRechazoCancelacion
                }).ToList(),
                [17])
        ]);

        return Result.Success(new ArchivoReporte(excel.Generar(libro), $"Asignaciones_{periodo.Sufijo}.xlsx"));
    }

    private async Task<List<AsignacionReporteRow>> CargarAsync(PeriodoReporte periodo, CancellationToken cancellationToken)
    {
        var movimientos = uow.Query<MovimientoCombustible>();

        return await uow.Query<Asignacion>()
            .Where(a => a.Status && a.FechaSalida >= periodo.Desde && a.FechaSalida < periodo.HastaExclusivo)
            .OrderBy(a => a.FechaSalida)
            .Select(a => new AsignacionReporteRow(
                a.FechaSalida,
                a.FechaRetornoEstimada,
                a.FechaRetornoReal,
                a.Estatus,
                a.TipoAsignacion,
                (a.RangoSolicitante != null ? a.RangoSolicitante + " " : "")
                    + a.SolicitantePersona!.Apellido + " " + a.SolicitantePersona.Nombre + " (" + a.SolicitantePersona.Identificacion + ")",
                a.CargoSolicitante,
                a.DependenciaSolicitante,
                a.Persona!.Apellido + " " + a.Persona.Nombre + " (" + a.Persona.Identificacion + ")",
                a.Cargo,
                (a.RangoChofer != null ? a.RangoChofer + " " : "")
                    + a.ChoferPersona!.Apellido + " " + a.ChoferPersona.Nombre + " (" + a.ChoferPersona.Identificacion + ")",
                a.Proposito,
                a.Destino,
                a.Vehiculo!.Placa,
                a.Vehiculo.Marca!.Nombre + " " + a.Vehiculo.Modelo!.Nombre,
                movimientos.Where(m => m.AsignacionId == a.Id && m.Status).Sum(m => (decimal?)m.Galones) ?? 0,
                a.MotivoRechazoCancelacion))
            .ToListAsync(cancellationToken);
    }

    private static EstadisticasAsignacionesViewModel Calcular(PeriodoReporte periodo, List<AsignacionReporteRow> rows)
    {
        var ahora = DateTime.Now;
        var finalizadas = rows.Where(r => r.Estatus == EstatusSolicitud.Finalizada && r.FechaRetornoReal.HasValue).ToList();
        var porIntervalo = rows.ToLookup(r => periodo.Intervalo(r.FechaSalida));

        return new EstadisticasAsignacionesViewModel
        {
            Periodo = PeriodoViewModel.From(periodo),
            Total = rows.Count,
            DuracionPromedioDias = finalizadas.Count > 0 ? Math.Round(finalizadas.Average(r => r.DiasDeUso!.Value), 2) : null,
            RetornosTardios = finalizadas.Count(r => r.RetornoTardio),
            VencidasSinRetorno = rows.Count(r => r.Estatus == EstatusSolicitud.Aprobada && r.FechaRetornoEstimada < ahora),
            GalonesDespachados = rows.Sum(r => r.GalonesDespachados),

            PorEstatus = Enum.GetValues<EstatusSolicitud>()
                .Select(e => new ConteoViewModel { Nombre = e.ToString(), Cantidad = rows.Count(r => r.Estatus == e) })
                .ToList(),

            PorTipoAsignacion = Enum.GetValues<TipoBeneficiario>()
                .Select(t => new ConteoViewModel { Nombre = t.ToString(), Cantidad = rows.Count(r => r.TipoAsignacion == t) })
                .ToList(),

            TopDependencias = Top(rows, r => r.Dependencia),
            TopVehiculos = Top(rows, r => $"{r.Placa} | {r.Vehiculo}"),

            Serie = periodo.Intervalos()
                .Select(fecha =>
                {
                    var delIntervalo = porIntervalo[fecha].ToList();
                    return new SerieAsignacionesViewModel
                    {
                        Fecha = fecha,
                        Total = delIntervalo.Count,
                        Pendientes = delIntervalo.Count(r => r.Estatus == EstatusSolicitud.Pendiente),
                        Aprobadas = delIntervalo.Count(r => r.Estatus is EstatusSolicitud.Aprobada or EstatusSolicitud.Finalizada),
                        NoAprobadas = delIntervalo.Count(r => r.Estatus is EstatusSolicitud.Rechazada or EstatusSolicitud.Cancelada)
                    };
                })
                .ToList()
        };
    }

    private static List<ConteoViewModel> Top(IEnumerable<AsignacionReporteRow> rows, Func<AsignacionReporteRow, string> clave) =>
        rows
            .GroupBy(clave)
            .Select(g => new ConteoViewModel { Nombre = g.Key, Cantidad = g.Count(), Galones = g.Sum(r => r.GalonesDespachados) })
            .OrderByDescending(c => c.Cantidad)
            .Take(TopCantidad)
            .ToList();
}
