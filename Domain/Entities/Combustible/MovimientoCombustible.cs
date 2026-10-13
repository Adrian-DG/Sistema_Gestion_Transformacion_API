using Domain.Abstraction;
using Domain.Entities.Historico;
using Domain.Entities.Organizacion;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Combustible
{
    /// <summary>
    /// Entrada (compra) o salida (despacho a vehículo asignado, a conjunto o canje de ticket)
    /// de combustible. La existencia es la suma de entradas menos salidas por tipo de combustible.
    /// </summary>
    public class MovimientoCombustible : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public ConceptoMovimientoCombustible Concepto { get; set; }
        public TipoCombustible TipoCombustible { get; set; }

        /// <summary>Siempre positivo; el signo lo determina <see cref="EsEntrada"/>.</summary>
        public decimal Galones { get; set; }
        public DateTime Fecha { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Proposito { get; set; }

        // Compra
        public string? Proveedor { get; set; }
        public string? NumeroFactura { get; set; }

        // Solicitante (despachos)
        public Guid? SolicitantePersonaId { get; set; }
        public virtual Persona? SolicitantePersona { get; set; }
        public Guid? DependenciaSolicitanteId { get; set; }
        public virtual Dependencia? DependenciaSolicitante { get; set; }

        // Despacho a vehículo asignado
        public Guid? AsignacionId { get; set; }
        public virtual Asignacion? Asignacion { get; set; }

        // Despacho a conjunto (grupo de vehículos / unidades de una misión)
        public string? DescripcionConjunto { get; set; }
        public int? CantidadUnidades { get; set; }

        // Canje de ticket
        public Guid? TicketCombustibleId { get; set; }
        public virtual TicketCombustible? TicketCombustible { get; set; }

        // Quién recibió el combustible (puede no estar registrado en el sistema)
        public Guid? ReceptorPersonaId { get; set; }
        public virtual Persona? ReceptorPersona { get; set; }
        public string? ReceptorNombre { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        [NotMapped]
        public bool EsEntrada => Concepto == ConceptoMovimientoCombustible.Compra;
    }
}
