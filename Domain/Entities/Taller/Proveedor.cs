using Domain.Abstraction;

namespace Domain.Entities.Taller
{
    /// <summary>
    /// Suplidor de piezas, de combustible o taller autorizado (o varios). No se elimina: Status en
    /// false lo desactiva.
    /// </summary>
    public class Proveedor : NamedEntityMetadata, IAuditableEntityMetadata
    {
        public string? Rnc { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        /// <summary>Persona de contacto en el proveedor.</summary>
        public string? Contacto { get; set; }
        /// <summary>Taller autorizado (ej. concesionario) donde se pueden hacer mantenimientos externos.</summary>
        public bool EsTallerAutorizado { get; set; }
        /// <summary>Suple combustible: solo estos aparecen al registrar una entrada de combustible.</summary>
        public bool EsSuplidorCombustible { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
