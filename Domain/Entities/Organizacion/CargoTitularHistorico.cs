using Domain.Abstraction;
using Domain.Entities.Historico;

namespace Domain.Entities.Organizacion
{
    public class CargoTitularHistorico : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public Guid CargoId { get; set; }
        public virtual Cargo? Cargo { get; set; }

        public Guid PersonaId { get; set; }
        public virtual Persona? Persona { get; set; }

        public DateTime FechaDesde { get; set; }

        /// <summary>Nulo mientras la persona siga siendo el titular del cargo.</summary>
        public DateTime? FechaHasta { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
