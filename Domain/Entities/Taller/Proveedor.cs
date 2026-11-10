using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Taller
{
    /// <summary>
    /// Proveedor según su <see cref="TipoProveedor"/>: taller autorizado, suplidor de combustible u otro
    /// (piezas, servicios). No se elimina: Status en false lo desactiva.
    /// </summary>
    public class Proveedor : NamedEntityMetadata, IAuditableEntityMetadata
    {
        public string? Rnc { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        /// <summary>Persona de contacto en el proveedor.</summary>
        public string? Contacto { get; set; }
        public TipoProveedor Tipo { get; set; } = TipoProveedor.Otro;

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
