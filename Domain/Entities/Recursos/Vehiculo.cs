using Domain.Abstraction;
using Domain.Entities.Historico;
using Domain.Entities.Misc;
using Domain.Enums;
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
        /// <summary>Número de ficha interno de la flota; único cuando se indica.</summary>
        public string? Ficha { get; set; }
        public int Fabricacion { get; set; }

        public Guid TipoId { get; set; }
        public virtual TipoVehiculo? Tipo { get; set; }
        public Guid ColorId { get; set; }
        public virtual Color? Color { get; set; }
        public Guid ModeloId { get; set; }
        public virtual Modelo? Modelo { get; set; }
        public Guid MarcaId { get; set; }
        public virtual Marca? Marca { get; set; }

        /// <summary>Solo lo cambia el taller; el estatus visible se resuelve con EstatusVehiculos.</summary>
        public EstadoVehiculo Estado { get; set; } = EstadoVehiculo.Operativo;

        public virtual ICollection<Asignacion>? Asignaciones { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public override string ToString()
        {
            return $"{Matricula} | {Marca?.Nombre} | {Modelo?.Nombre}";
        }

    }
}
