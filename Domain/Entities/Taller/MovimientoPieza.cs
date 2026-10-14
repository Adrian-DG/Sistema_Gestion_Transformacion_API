using Domain.Abstraction;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Taller
{
    /// <summary>
    /// Entrada o salida de una pieza (kardex). La existencia de la pieza es la suma de entradas menos
    /// salidas. Siempre guarda el costo unitario, así cada orden de taller tiene su costo.
    /// </summary>
    public class MovimientoPieza : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public ConceptoMovimientoPieza Concepto { get; set; }

        public Guid PiezaId { get; set; }
        public virtual Pieza? Pieza { get; set; }

        /// <summary>Siempre positiva; el signo lo determina <see cref="EsEntrada"/>.</summary>
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>Entrada al inventario a la que pertenece la línea (concepto Entrada).</summary>
        public Guid? EntradaPiezaId { get; set; }
        public virtual EntradaPieza? EntradaPieza { get; set; }

        /// <summary>Orden de taller (conceptos CompraDirecta, Consumo y Devolucion).</summary>
        public Guid? SolicitudTallerId { get; set; }
        public virtual SolicitudTaller? SolicitudTaller { get; set; }

        // Compra directa: a quién se compró. El recibo es un Adjunto de este movimiento.
        public Guid? ProveedorId { get; set; }
        public virtual Proveedor? Proveedor { get; set; }
        public string? NumeroFactura { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        [NotMapped]
        public bool EsEntrada => Concepto != ConceptoMovimientoPieza.Consumo;
    }
}
