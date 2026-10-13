using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Organizacion
{
    /// <summary>
    /// Dirección o dependencia institucional (ej. J-4, Dirección de TIC).
    /// </summary>
    public class Dependencia : NamedEntityMetadata, IAuditableEntityMetadata
    {
        public string? Siglas { get; set; }
        public InstitucionEnum Institucion { get; set; }

        public virtual ICollection<Cargo>? Cargos { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
