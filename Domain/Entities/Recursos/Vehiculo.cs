using Domain.Abstraction;
using Domain.Entities.Historico;
using Domain.Entities.Misc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Recursos
{
    public class Vehiculo : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public required string Chasis { get; set; }
        public required string Matricula { get; set; }
        public required string Placa { get; set; }
        public int Fabricacion { get; set; }

        public Guid TipoId { get; set; }
        public virtual TipoVehiculo? Tipo { get; set; }
        public Guid ColorId { get; set; }
        public virtual Color? Color { get; set; }
        public Guid ModeloId { get; set; }
        public virtual Modelo? Modelo { get; set; }
        public Guid MarcaId { get; set; }
        public virtual Marca? Marca { get; set; }

        public virtual ICollection<Asignacion>? Asignaciones { get; set; }

        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }

    }
}
