namespace Domain.Enums
{
    /// <summary>
    /// Entradas: Entrada (compra al inventario), CompraDirecta (pieza comprada para una orden de taller
    /// porque no había existencia suficiente) y Devolucion (piezas de una orden cancelada que vuelven al
    /// inventario). Salida: Consumo (piezas utilizadas en una orden de taller).
    /// </summary>
    public enum ConceptoMovimientoPieza
    {
        Entrada = 1,
        CompraDirecta = 2,
        Consumo = 3,
        Devolucion = 4
    }
}
