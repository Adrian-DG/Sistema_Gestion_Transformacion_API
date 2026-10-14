using Domain.Abstraction;
using Domain.Entities.Recursos;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Taller
{
    /// <summary>
    /// Reactivación excepcional de un vehículo dado de baja. Queda en su historial de mantenimientos;
    /// quien la autorizó es <see cref="CreatedBy"/>.
    /// </summary>
    public class ReactivacionVehiculo : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public Guid VehiculoId { get; set; }
        public virtual Vehiculo? Vehiculo { get; set; }

        public DateTime Fecha { get; set; }

        [DataType(DataType.MultilineText)]
        public required string Motivo { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
