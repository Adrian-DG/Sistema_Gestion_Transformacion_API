using Domain.Abstraction;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Taller
{
    /// <summary>
    /// Compra de piezas al inventario: una factura con varias líneas. Cada línea es un
    /// <see cref="MovimientoPieza"/> de concepto Entrada con su costo. La factura es un Adjunto.
    /// </summary>
    public class EntradaPieza : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public Guid ProveedorId { get; set; }
        public virtual Proveedor? Proveedor { get; set; }
        public string? NumeroFactura { get; set; }
        public DateTime Fecha { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Observacion { get; set; }

        public virtual ICollection<MovimientoPieza> Lineas { get; set; } = [];

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
