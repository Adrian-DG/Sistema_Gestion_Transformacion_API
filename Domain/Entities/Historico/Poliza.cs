using Domain.Abstraction;
using Domain.Entities.Misc;
using Domain.Entities.Recursos;

namespace Domain.Entities.Historico
{
    /// <summary>
    /// Póliza de seguro de un vehículo. No se eliminan: al renovar se registra una póliza nueva y la
    /// anterior queda en el historial del vehículo.
    /// </summary>
    public class Poliza : BaseEntityMetadata, IAuditableEntityMetadata
    {
        /// <summary>Días antes del vencimiento en que la póliza se considera "por vencer".</summary>
        public const int DiasAvisoVencimiento = 30;

        public required string Numero { get; set; }
        public DateOnly FechaExpedicion { get; set; }
        public DateOnly FechaEfectividad { get; set; }
        public DateOnly FechaVencimiento { get; set; }

        public Guid VehiculoId { get; set; }
        public virtual Vehiculo? Vehiculo { get; set; }

        public Guid AseguradoraId { get; set; }
        public virtual Aseguradora? Aseguradora { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        /// <summary>Días desde <paramref name="hoy"/> hasta el vencimiento (negativo si ya venció).</summary>
        public static int DiasRestantes(DateOnly vencimiento, DateOnly hoy) => vencimiento.DayNumber - hoy.DayNumber;

        /// <summary>
        /// Vigente, PorVencer (vence dentro de <see cref="DiasAvisoVencimiento"/> días) o Vencida. El día del
        /// vencimiento la póliza ya se considera vencida.
        /// </summary>
        public static EstadoCobertura Cobertura(DateOnly vencimiento, DateOnly hoy) => DiasRestantes(vencimiento, hoy) switch
        {
            <= 0 => EstadoCobertura.Vencida,
            <= DiasAvisoVencimiento => EstadoCobertura.PorVencer,
            _ => EstadoCobertura.Vigente
        };
    }

    public enum EstadoCobertura
    {
        Vigente = 1,
        PorVencer = 2,
        Vencida = 3
    }
}
