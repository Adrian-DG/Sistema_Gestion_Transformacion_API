using Domain.Abstraction;
using Domain.Entities.Historico;
using Domain.Entities.Misc;
using Domain.Entities.Taller;
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

        /// <summary>Último kilometraje conocido: el de su registro o el del último ingreso al taller.</summary>
        public int Kilometraje { get; set; }

        // Garantía del fabricante: vence por fecha o por kilometraje, lo que ocurra primero. Para
        // conservarla, los mantenimientos se hacen en el taller autorizado (el concesionario).
        public DateOnly? GarantiaVence { get; set; }
        public int? GarantiaKilometraje { get; set; }
        public Guid? GarantiaTallerId { get; set; }
        public virtual Proveedor? GarantiaTaller { get; set; }

        /// <summary>Garantía vigente a la fecha indicada con el kilometraje actual.</summary>
        public bool EnGarantia(DateOnly hoy) =>
            GarantiaVence is { } vence && vence >= hoy && (GarantiaKilometraje is not { } limite || Kilometraje <= limite);

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
