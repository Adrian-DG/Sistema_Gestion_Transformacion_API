using Domain.Abstraction;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Entities.Recursos;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Taller
{
    /// <summary>
    /// Solicitud de taller y orden de trabajo del vehículo; las finalizadas forman su historial de
    /// mantenimientos. Se registra Pendiente; al ingresar se le asigna un mecánico y el vehículo pasa a
    /// En mantenimiento; al finalizar vuelve a operativo o, si el mecánico dictamina la baja, a inactivo.
    /// Las piezas utilizadas son sus <see cref="MovimientoPieza"/> de consumo.
    /// </summary>
    public class SolicitudTaller : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public const string CodigoSecuencia = "SolicitudTaller";

        public static string FormatearNumero(int anio, long secuencia) => $"ST-{anio}-{secuencia:D6}";

        public required string Numero { get; set; }
        public TipoSolicitudTaller Tipo { get; set; }
        public EstatusSolicitudTaller Estatus { get; set; }

        public Guid VehiculoId { get; set; }
        public virtual Vehiculo? Vehiculo { get; set; }

        public Guid SolicitantePersonaId { get; set; }
        public virtual Persona? SolicitantePersona { get; set; }

        [DataType(DataType.MultilineText)]
        public required string Motivo { get; set; }
        /// <summary>Kilometraje al registrar la solicitud o, si se indica, al ingresar.</summary>
        public int? Kilometraje { get; set; }

        // Ingreso
        public Guid? MecanicoId { get; set; }
        public virtual Mecanico? Mecanico { get; set; }
        public DateTime? FechaIngreso { get; set; }

        // Cierre
        public DateTime? FechaSalida { get; set; }
        [DataType(DataType.MultilineText)]
        public string? Diagnostico { get; set; }
        [DataType(DataType.MultilineText)]
        public string? TrabajoRealizado { get; set; }
        /// <summary>Dictamen del mecánico: el vehículo se da de baja (pasa a inactivo).</summary>
        public bool ProcedeBaja { get; set; }

        [DataType(DataType.MultilineText)]
        public string? MotivoRechazoCancelacion { get; set; }

        public virtual ICollection<MovimientoPieza> Movimientos { get; set; } = [];

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public bool Activa => Estatus is EstatusSolicitudTaller.Pendiente or EstatusSolicitudTaller.EnTaller;

        public Result Ingresar(Guid mecanicoId, DateTime fecha, int? kilometraje)
        {
            if (Estatus != EstatusSolicitudTaller.Pendiente)
            {
                return Result.Failure(TallerErrors.InvalidStatus(Numero, Estatus.ToString()));
            }

            Estatus = EstatusSolicitudTaller.EnTaller;
            MecanicoId = mecanicoId;
            FechaIngreso = fecha;
            if (kilometraje is not null) Kilometraje = kilometraje;
            return Result.Success();
        }

        public Result Finalizar(string diagnostico, string trabajoRealizado, bool procedeBaja, DateTime fecha)
        {
            if (Estatus != EstatusSolicitudTaller.EnTaller)
            {
                return Result.Failure(TallerErrors.InvalidStatus(Numero, Estatus.ToString()));
            }

            Estatus = EstatusSolicitudTaller.Finalizada;
            Diagnostico = diagnostico;
            TrabajoRealizado = trabajoRealizado;
            ProcedeBaja = procedeBaja;
            FechaSalida = fecha;
            return Result.Success();
        }

        public Result Rechazar(string motivo)
        {
            if (Estatus != EstatusSolicitudTaller.Pendiente)
            {
                return Result.Failure(TallerErrors.InvalidStatus(Numero, Estatus.ToString()));
            }

            Estatus = EstatusSolicitudTaller.Rechazada;
            MotivoRechazoCancelacion = motivo;
            return Result.Success();
        }

        /// <summary>Si estaba en el taller, el vehículo sale sin cambios y las piezas vuelven al inventario.</summary>
        public Result Cancelar(string motivo, DateTime fecha)
        {
            if (!Activa)
            {
                return Result.Failure(TallerErrors.InvalidStatus(Numero, Estatus.ToString()));
            }

            if (Estatus == EstatusSolicitudTaller.EnTaller) FechaSalida = fecha;
            Estatus = EstatusSolicitudTaller.Cancelada;
            MotivoRechazoCancelacion = motivo;
            return Result.Success();
        }
    }
}
