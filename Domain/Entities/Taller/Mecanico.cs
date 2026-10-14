using Domain.Abstraction;
using Domain.Entities.Historico;

namespace Domain.Entities.Taller
{
    /// <summary>
    /// Personal que trabaja como mecánico. Es una <see cref="Historico.Persona"/>: los militares vienen de
    /// SIPFFAA y los civiles se registran a mano. Status en false lo desactiva.
    /// </summary>
    public class Mecanico : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public Guid PersonaId { get; set; }
        public virtual Persona? Persona { get; set; }

        public string? Especialidad { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
