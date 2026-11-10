using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Recursos
{
    /// <summary>
    /// Fotografía del vehículo en una de sus vistas. Las cuatro se toman al ingresarlo a la flota; al
    /// reemplazar una, la anterior queda con Status en false como historial (no se elimina).
    /// </summary>
    public class FotoVehiculo : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public Guid VehiculoId { get; set; }
        public virtual Vehiculo? Vehiculo { get; set; }

        public VistaVehiculo Vista { get; set; }

        public required byte[] File { get; set; }
        public required string NombreArchivo { get; set; }
        public required string ContentType { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
