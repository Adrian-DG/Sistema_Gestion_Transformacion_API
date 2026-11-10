namespace Domain.Enums
{
    /// <summary>Tipo de pieza o insumo del inventario del taller.</summary>
    public enum TipoPieza
    {
        Otro = 0,
        Repuesto = 1,
        /// <summary>Aceites, refrigerantes y otros fluidos.</summary>
        Lubricante = 2,
        Filtro = 3,
        Neumatico = 4,
        Bateria = 5,
        Electrico = 6,
        Frenos = 7,
        Accesorio = 8
    }
}
