using Application.Contracts;
using Domain.Entities.Taller;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Taller.Inventario;

/// <summary>
/// Existencia y costo de las piezas a partir de su kardex. Las entradas (compras, compras directas y
/// devoluciones) suman y los consumos restan. El costo promedio pondera las compras por su cantidad.
/// </summary>
public static class ExistenciaPiezas
{
    public static async Task<(decimal Existencia, decimal CostoPromedio)> ObtenerExistenciaAsync(
        this IUnitOfWork uow, Guid piezaId, CancellationToken cancellationToken)
    {
        var movimientos = uow.Query<MovimientoPieza>().Where(m => m.PiezaId == piezaId && m.Status);

        var existencia = await movimientos
            .SumAsync(m => m.Concepto == ConceptoMovimientoPieza.Consumo ? -m.Cantidad : m.Cantidad, cancellationToken);

        var compras = movimientos.Where(m =>
            m.Concepto == ConceptoMovimientoPieza.Entrada || m.Concepto == ConceptoMovimientoPieza.CompraDirecta);
        var cantidadComprada = await compras.SumAsync(m => m.Cantidad, cancellationToken);
        var costoComprado = await compras.SumAsync(m => m.Cantidad * m.CostoUnitario, cancellationToken);

        return (existencia, cantidadComprada > 0 ? Math.Round(costoComprado / cantidadComprada, 2) : 0);
    }
}
