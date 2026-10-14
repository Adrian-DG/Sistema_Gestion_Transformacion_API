using Domain.Abstraction;

namespace Domain.Entities.Taller
{
    /// <summary>
    /// Pieza o insumo del inventario del taller. La existencia es la suma de entradas menos salidas de
    /// sus <see cref="MovimientoPieza"/>; no se guarda en la pieza. Status en false la desactiva.
    /// </summary>
    public class Pieza : NamedEntityMetadata, IAuditableEntityMetadata
    {
        public required string Codigo { get; set; }
        /// <summary>Código de barras del fabricante o del proveedor (opcional); único cuando se indica.</summary>
        public string? CodigoBarras { get; set; }
        public string? Descripcion { get; set; }
        /// <summary>Unidad, galón, litro, juego…</summary>
        public required string UnidadMedida { get; set; }
        /// <summary>Por debajo de esta existencia la pieza se marca para reponer.</summary>
        public decimal ExistenciaMinima { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
