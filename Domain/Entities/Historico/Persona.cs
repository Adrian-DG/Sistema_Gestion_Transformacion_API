using Domain.Abstraction;
using Domain.Entities.Misc;
using Domain.Entities.Organizacion;
using Domain.Enums;

namespace Domain.Entities.Historico
{
    /// <summary>
    /// Militar, asimilado o civil que puede solicitar o recibir vehículos y combustible.
    /// Rango e institución solo aplican al personal militar.
    /// </summary>
    public class Persona : PersonEntityMetadata, IAuditableEntityMetadata
    {
        public TipoPersona TipoPersona { get; set; }

        public Guid? RangoId { get; set; }
        public virtual Rango? Rango { get; set; }

        public InstitucionEnum? Institucion { get; set; }

        public Guid? DependenciaId { get; set; }
        public virtual Dependencia? Dependencia { get; set; }

        public string? Telefono { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
