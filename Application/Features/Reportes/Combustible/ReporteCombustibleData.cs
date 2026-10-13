using Application.Contracts;
using Domain.Entities.Combustible;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reportes.Combustible;

/// <summary>Movimiento de combustible ya resuelto para reportes.</summary>
public record MovimientoReporteRow(
    DateTime Fecha,
    ConceptoMovimientoCombustible Concepto,
    TipoCombustible TipoCombustible,
    decimal Galones,
    string? TipoDespacho,
    string? Beneficiario,
    string? Dependencia,
    string? Referencia,
    string? Proveedor,
    string? Receptor,
    string? Proposito)
{
    public bool EsEntrada => Concepto == ConceptoMovimientoCombustible.Compra;
    public string ConceptoDescripcion => ReporteCombustibleData.DescribirConcepto(Concepto);
}

public record DatosReporteCombustible(
    PeriodoReporte Periodo,
    IReadOnlyList<MovimientoReporteRow> Movimientos,
    IReadOnlyDictionary<TipoCombustible, decimal> ExistenciaInicial);

public static class ReporteCombustibleData
{
    public const string TipoDespachoPersona = "Persona";
    public const string TipoDespachoCargo = "Cargo";
    public const string TipoDespachoConjunto = "Conjunto";

    public static string DescribirConcepto(ConceptoMovimientoCombustible concepto) => concepto switch
    {
        ConceptoMovimientoCombustible.Compra => "Compra",
        ConceptoMovimientoCombustible.DespachoVehiculoAsignado => "Vehículo asignado",
        ConceptoMovimientoCombustible.DespachoConjunto => "Conjunto",
        ConceptoMovimientoCombustible.Ticket => "Ticket",
        _ => concepto.ToString()
    };

    public static async Task<DatosReporteCombustible> CargarAsync(
        this IUnitOfWork uow, PeriodoReporte periodo, TipoCombustible? tipoCombustible, CancellationToken cancellationToken)
    {
        var movimientos = uow.Query<MovimientoCombustible>()
            .Where(m => m.Status && (tipoCombustible == null || m.TipoCombustible == tipoCombustible));

        var existenciaInicial = await movimientos
            .Where(m => m.Fecha < periodo.Desde)
            .GroupBy(m => m.TipoCombustible)
            .Select(g => new
            {
                Tipo = g.Key,
                Galones = g.Sum(m => m.Concepto == ConceptoMovimientoCombustible.Compra ? m.Galones : -m.Galones)
            })
            .ToDictionaryAsync(x => x.Tipo, x => x.Galones, cancellationToken);

        var data = await movimientos
            .Where(m => m.Fecha >= periodo.Desde && m.Fecha < periodo.HastaExclusivo)
            .OrderBy(m => m.Fecha)
            .Select(m => new
            {
                m.Fecha,
                m.Concepto,
                m.TipoCombustible,
                m.Galones,
                m.Proposito,
                m.Proveedor,
                m.NumeroFactura,
                m.DescripcionConjunto,
                m.CantidadUnidades,
                AsignacionTipo = m.Asignacion != null ? (TipoBeneficiario?)m.Asignacion.TipoAsignacion : null,
                AsignacionCargo = m.Asignacion != null && m.Asignacion.Cargo != null ? m.Asignacion.Cargo.Nombre : null,
                AsignacionPersona = m.Asignacion != null && m.Asignacion.Persona != null
                    ? m.Asignacion.Persona.Apellido + " " + m.Asignacion.Persona.Nombre : null,
                Placa = m.Asignacion != null ? m.Asignacion.Vehiculo!.Placa : null,
                TicketTipo = m.TicketCombustible != null ? (TipoBeneficiario?)m.TicketCombustible.SolicitudTicket!.TipoBeneficiario : null,
                TicketCargo = m.TicketCombustible != null && m.TicketCombustible.SolicitudTicket!.Cargo != null
                    ? m.TicketCombustible.SolicitudTicket.Cargo.Nombre : null,
                TicketPersona = m.TicketCombustible != null && m.TicketCombustible.SolicitudTicket!.Persona != null
                    ? m.TicketCombustible.SolicitudTicket.Persona.Apellido + " " + m.TicketCombustible.SolicitudTicket.Persona.Nombre : null,
                TicketNumero = m.TicketCombustible != null ? m.TicketCombustible.Numero : null,
                Dependencia = m.DependenciaSolicitante != null
                    ? m.DependenciaSolicitante.Nombre
                    : m.TicketCombustible != null ? m.TicketCombustible.SolicitudTicket!.DependenciaSolicitante!.Nombre : null,
                Receptor = m.ReceptorPersona != null
                    ? m.ReceptorPersona.Apellido + " " + m.ReceptorPersona.Nombre
                    : m.ReceptorNombre
            })
            .ToListAsync(cancellationToken);

        var rows = data.Select(m =>
        {
            var tipoBeneficiario = m.Concepto switch
            {
                ConceptoMovimientoCombustible.DespachoVehiculoAsignado => m.AsignacionTipo,
                ConceptoMovimientoCombustible.Ticket => m.TicketTipo,
                _ => null
            };

            var tipoDespacho = m.Concepto switch
            {
                ConceptoMovimientoCombustible.Compra => null,
                ConceptoMovimientoCombustible.DespachoConjunto => TipoDespachoConjunto,
                _ => tipoBeneficiario == TipoBeneficiario.Cargo ? TipoDespachoCargo : TipoDespachoPersona
            };

            var beneficiario = m.Concepto switch
            {
                ConceptoMovimientoCombustible.Compra => null,
                ConceptoMovimientoCombustible.DespachoConjunto => m.DescripcionConjunto,
                ConceptoMovimientoCombustible.DespachoVehiculoAsignado => m.AsignacionCargo ?? m.AsignacionPersona,
                _ => m.TicketCargo ?? m.TicketPersona
            };

            var referencia = m.Concepto switch
            {
                ConceptoMovimientoCombustible.Compra => m.NumeroFactura is null ? null : $"Factura {m.NumeroFactura}",
                ConceptoMovimientoCombustible.DespachoVehiculoAsignado => $"Placa {m.Placa}",
                ConceptoMovimientoCombustible.DespachoConjunto => $"{m.CantidadUnidades} unidades",
                _ => $"Ticket {m.TicketNumero}"
            };

            return new MovimientoReporteRow(
                m.Fecha, m.Concepto, m.TipoCombustible, m.Galones, tipoDespacho, beneficiario,
                m.Dependencia, referencia, m.Proveedor, m.Receptor, m.Proposito);
        }).ToList();

        return new DatosReporteCombustible(periodo, rows, existenciaInicial);
    }
}
