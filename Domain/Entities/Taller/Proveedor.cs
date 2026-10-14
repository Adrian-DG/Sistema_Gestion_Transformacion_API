using Domain.Abstraction;

namespace Domain.Entities.Taller
{
    /// <summary>Suplidor de piezas. No se elimina: Status en false lo desactiva.</summary>
    public class Proveedor : NamedEntityMetadata, IAuditableEntityMetadata
    {
        public string? Rnc { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        /// <summary>Persona de contacto en el proveedor.</summary>
        public string? Contacto { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
