using Application.Contracts;
using Application.Contracts.Reportes;
using Domain.Common;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Reportes.Combustible;

public record GetEstadisticasCombustibleQuery(
    TipoPeriodo Periodo, DateOnly? Fecha, DateOnly? Desde, DateOnly? Hasta, TipoCombustible? TipoCombustible)
    : IRequest<Result<EstadisticasCombustibleViewModel>>, IPeriodoRequest;

public record GetReporteExcelCombustibleQuery(
    TipoPeriodo Periodo, DateOnly? Fecha, DateOnly? Desde, DateOnly? Hasta, TipoCombustible? TipoCombustible)
    : IRequest<Result<ArchivoReporte>>, IPeriodoRequest;

public class GetEstadisticasCombustibleQueryValidator : PeriodoRequestValidator<GetEstadisticasCombustibleQuery>;

public class GetReporteExcelCombustibleQueryValidator : PeriodoRequestValidator<GetReporteExcelCombustibleQuery>;

public class EstadisticasCombustibleViewModel
{
    public required PeriodoViewModel Periodo { get; set; }
    public List<ResumenCombustibleViewModel> Resumen { get; set; } = [];

    /// <summary>Salidas agrupadas por Persona, Cargo o Conjunto.</summary>
    public List<SalidaAgrupadaViewModel> SalidasPorTipoDespacho { get; set; } = [];

    /// <summary>Salidas agrupadas por concepto: vehículo asignado, conjunto o ticket.</summary>
    public List<SalidaAgrupadaViewModel> SalidasPorConcepto { get; set; } = [];

    public List<SerieCombustibleViewModel> Serie { get; set; } = [];
    public List<ConteoViewModel> TopDependencias { get; set; } = [];
    public List<ConteoViewModel> TopBeneficiarios { get; set; } = [];
}

public class ResumenCombustibleViewModel
{
    public required string TipoCombustible { get; set; }
    public decimal ExistenciaInicial { get; set; }
    public decimal Entradas { get; set; }
    public int CantidadEntradas { get; set; }
    public decimal Salidas { get; set; }
    public int CantidadSalidas { get; set; }
    public decimal ExistenciaFinal => ExistenciaInicial + Entradas - Salidas;
}

public class SalidaAgrupadaViewModel
{
    public required string Grupo { get; set; }
    public required string TipoCombustible { get; set; }
    public int Cantidad { get; set; }
    public decimal Galones { get; set; }
}

public class SerieCombustibleViewModel
{
    public DateOnly Fecha { get; set; }
    public required string TipoCombustible { get; set; }
    public decimal Entradas { get; set; }
    public decimal Salidas { get; set; }
}

public class ReporteCombustibleQueryHandler(IUnitOfWork uow, IExcelReportGenerator excel) :
    IRequestHandler<GetEstadisticasCombustibleQuery, Result<EstadisticasCombustibleViewModel>>,
    IRequestHandler<GetReporteExcelCombustibleQuery, Result<ArchivoReporte>>
{
    private const int TopCantidad = 10;

    public async Task<Result<EstadisticasCombustibleViewModel>> Handle(GetEstadisticasCombustibleQuery request, CancellationToken cancellationToken)
    {
        var datos = await uow.CargarAsync(PeriodoReporte.Resolver(request), request.TipoCombustible, cancellationToken);
        return Result.Success(Calcular(datos, request.TipoCombustible));
    }

    public async Task<Result<ArchivoReporte>> Handle(GetReporteExcelCombustibleQuery request, CancellationToken cancellationToken)
    {
        var periodo = PeriodoReporte.Resolver(request);
        var datos = await uow.CargarAsync(periodo, request.TipoCombustible, cancellationToken);
        var estadisticas = Calcular(datos, request.TipoCombustible);

        var libro = new LibroReporte("Reporte de operaciones de combustible", periodo.Descripcion,
        [
            new HojaReporte("Resumen", "Entradas y salidas por tipo de combustible (galones)",
                ["Combustible", "Existencia inicial", "Entradas", "Cant. entradas", "Salidas", "Cant. salidas", "Existencia final"],
                estadisticas.Resumen.Select(r => new object?[]
                {
                    r.TipoCombustible, r.ExistenciaInicial, r.Entradas, r.CantidadEntradas, r.Salidas, r.CantidadSalidas, r.ExistenciaFinal
                }).ToList(),
                [2, 3, 4, 5]),

            new HojaReporte("Por tipo de despacho", "Despachos por tipo (persona, cargo, conjunto)",
                ["Tipo de despacho", "Combustible", "Despachos", "Galones"],
                estadisticas.SalidasPorTipoDespacho.Select(s => new object?[] { s.Grupo, s.TipoCombustible, s.Cantidad, s.Galones }).ToList(),
                [2, 3]),

            new HojaReporte("Por concepto", "Despachos por concepto",
                ["Concepto", "Combustible", "Despachos", "Galones"],
                estadisticas.SalidasPorConcepto.Select(s => new object?[] { s.Grupo, s.TipoCombustible, s.Cantidad, s.Galones }).ToList(),
                [2, 3]),

            new HojaReporte("Por dependencia", $"Dependencias con mayor consumo (top {TopCantidad})",
                ["Dependencia", "Despachos", "Galones"],
                estadisticas.TopDependencias.Select(d => new object?[] { d.Nombre, d.Cantidad, d.Galones }).ToList(),
                [1, 2]),

            new HojaReporte("Serie", $"Entradas y salidas por {(periodo.Agrupacion == AgrupacionSerie.Mes ? "mes" : "día")}",
                ["Fecha", "Combustible", "Entradas", "Salidas"],
                estadisticas.Serie.Select(s => new object?[] { s.Fecha, s.TipoCombustible, s.Entradas, s.Salidas }).ToList(),
                [2, 3]),

            new HojaReporte("Movimientos", "Detalle de movimientos",
                ["Fecha", "Concepto", "Tipo de despacho", "Combustible", "Entrada (gal)", "Salida (gal)",
                 "Beneficiario", "Dependencia solicitante", "Referencia", "Proveedor", "Recibido por", "Propósito"],
                datos.Movimientos.Select(m => new object?[]
                {
                    m.Fecha, m.ConceptoDescripcion, m.TipoDespacho, m.TipoCombustible.ToString(),
                    m.EsEntrada ? m.Galones : null, m.EsEntrada ? null : m.Galones,
                    m.Beneficiario, m.Dependencia, m.Referencia, m.Proveedor, m.Receptor, m.Proposito
                }).ToList(),
                [4, 5])
        ]);

        return Result.Success(new ArchivoReporte(excel.Generar(libro), $"Combustible_{periodo.Sufijo}.xlsx"));
    }

    private static EstadisticasCombustibleViewModel Calcular(DatosReporteCombustible datos, TipoCombustible? filtro)
    {
        var periodo = datos.Periodo;
        var movimientos = datos.Movimientos;
        var salidas = movimientos.Where(m => !m.EsEntrada).ToList();
        TipoCombustible[] tipos = filtro.HasValue ? [filtro.Value] : Enum.GetValues<TipoCombustible>();

        var galonesPorIntervalo = movimientos
            .GroupBy(m => (Fecha: periodo.Intervalo(m.Fecha), m.TipoCombustible, m.EsEntrada))
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Galones));

        return new EstadisticasCombustibleViewModel
        {
            Periodo = PeriodoViewModel.From(periodo),

            Resumen = tipos.Select(tipo =>
            {
                var delTipo = movimientos.Where(m => m.TipoCombustible == tipo).ToList();
                return new ResumenCombustibleViewModel
                {
                    TipoCombustible = tipo.ToString(),
                    ExistenciaInicial = datos.ExistenciaInicial.GetValueOrDefault(tipo),
                    Entradas = delTipo.Where(m => m.EsEntrada).Sum(m => m.Galones),
                    CantidadEntradas = delTipo.Count(m => m.EsEntrada),
                    Salidas = delTipo.Where(m => !m.EsEntrada).Sum(m => m.Galones),
                    CantidadSalidas = delTipo.Count(m => !m.EsEntrada)
                };
            }).ToList(),

            SalidasPorTipoDespacho = Agrupar(salidas, m => m.TipoDespacho!),
            SalidasPorConcepto = Agrupar(salidas, m => m.ConceptoDescripcion),

            Serie = periodo.Intervalos()
                .SelectMany(fecha => tipos.Select(tipo => new SerieCombustibleViewModel
                {
                    Fecha = fecha,
                    TipoCombustible = tipo.ToString(),
                    Entradas = galonesPorIntervalo.GetValueOrDefault((fecha, tipo, true)),
                    Salidas = galonesPorIntervalo.GetValueOrDefault((fecha, tipo, false))
                }))
                .ToList(),

            TopDependencias = Top(salidas, m => m.Dependencia),
            TopBeneficiarios = Top(salidas, m => m.Beneficiario)
        };
    }

    private static List<SalidaAgrupadaViewModel> Agrupar(IEnumerable<MovimientoReporteRow> salidas, Func<MovimientoReporteRow, string> grupo) =>
        salidas
            .GroupBy(m => (Grupo: grupo(m), m.TipoCombustible))
            .Select(g => new SalidaAgrupadaViewModel
            {
                Grupo = g.Key.Grupo,
                TipoCombustible = g.Key.TipoCombustible.ToString(),
                Cantidad = g.Count(),
                Galones = g.Sum(m => m.Galones)
            })
            .OrderBy(s => s.Grupo).ThenBy(s => s.TipoCombustible)
            .ToList();

    private static List<ConteoViewModel> Top(IEnumerable<MovimientoReporteRow> salidas, Func<MovimientoReporteRow, string?> clave) =>
        salidas
            .Where(m => !string.IsNullOrWhiteSpace(clave(m)))
            .GroupBy(m => clave(m)!)
            .Select(g => new ConteoViewModel { Nombre = g.Key, Cantidad = g.Count(), Galones = g.Sum(m => m.Galones) })
            .OrderByDescending(c => c.Galones)
            .Take(TopCantidad)
            .ToList();
}
