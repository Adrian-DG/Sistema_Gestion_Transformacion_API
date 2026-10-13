using Application.Contracts;
using Domain.Entities.Combustible;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible;

public class ExistenciaCombustibleViewModel
{
    public TipoCombustible TipoCombustible { get; set; }

    /// <summary>Entradas menos salidas registradas.</summary>
    public decimal Existencia { get; set; }

    /// <summary>Galones en tickets aprobados que aún no se han utilizado.</summary>
    public decimal ComprometidoEnTickets { get; set; }

    /// <summary>Galones que se pueden despachar o comprometer en nuevos tickets.</summary>
    public decimal DisponibleParaDespacho => Existencia - ComprometidoEnTickets;
}

public static class ExistenciaCombustible
{
    public static async Task<ExistenciaCombustibleViewModel> ObtenerExistenciaAsync(
        this IUnitOfWork uow, TipoCombustible tipo, CancellationToken cancellationToken)
    {
        var existencia = await uow.Query<MovimientoCombustible>()
            .Where(m => m.TipoCombustible == tipo && m.Status)
            .SumAsync(m => m.Concepto == ConceptoMovimientoCombustible.Compra ? m.Galones : -m.Galones, cancellationToken);

        var comprometido = await uow.Query<TicketCombustible>()
            .Where(t => t.TipoCombustible == tipo && t.Estatus == EstatusTicket.Disponible)
            .SumAsync(t => t.Galones, cancellationToken);

        return new ExistenciaCombustibleViewModel
        {
            TipoCombustible = tipo,
            Existencia = existencia,
            ComprometidoEnTickets = comprometido
        };
    }
}
