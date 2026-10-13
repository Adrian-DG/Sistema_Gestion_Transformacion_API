using Domain.Abstraction;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Organizacion;
using Domain.Entities.Recursos;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Historico
{
    /// <summary>
    /// Solicitud y asignación de un vehículo a un cargo o a una persona autorizada.
    /// Se crea en estatus Pendiente; al aprobarse reserva el vehículo para el período
    /// [FechaSalida, FechaRetornoEstimada] y se finaliza al registrar el retorno real.
    /// </summary>
    public class Asignacion : BaseEntityMetadata, IAuditableEntityMetadata
    {
        /// <summary>
        /// Horas previas a la salida en que un vehículo con asignación aprobada deja de
        /// estar disponible para otras asignaciones.
        /// </summary>
        public const int HorasReservaPrevia = 24;

        public EstatusSolicitud Estatus { get; set; }

        // A quién se asigna
        public TipoBeneficiario TipoAsignacion { get; set; }
        public Guid? CargoId { get; set; }
        public virtual Cargo? Cargo { get; set; }
        public Guid? PersonaId { get; set; }
        public virtual Persona? Persona { get; set; }

        // Quién lo solicita y para qué
        public Guid SolicitantePersonaId { get; set; }
        public virtual Persona? SolicitantePersona { get; set; }
        public Guid DependenciaSolicitanteId { get; set; }
        public virtual Dependencia? DependenciaSolicitante { get; set; }

        [DataType(DataType.MultilineText)]
        public required string Proposito { get; set; }
        public string? Destino { get; set; }

        public Guid VehiculoId { get; set; }
        public virtual Vehiculo? Vehiculo { get; set; }

        public DateTime FechaSalida { get; set; }
        public DateTime FechaRetornoEstimada { get; set; }
        public DateTime? FechaRetornoReal { get; set; }

        public DateTime? FechaAprobacion { get; set; }
        public Guid? AprobadoPor { get; set; }

        [DataType(DataType.MultilineText)]
        public string? MotivoRechazoCancelacion { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public Result Aprobar(Guid usuarioId, DateTime fecha)
        {
            if (Estatus != EstatusSolicitud.Pendiente)
            {
                return Result.Failure(AsignacionErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            Estatus = EstatusSolicitud.Aprobada;
            FechaAprobacion = fecha;
            AprobadoPor = usuarioId;
            return Result.Success();
        }

        public Result Rechazar(string motivo)
        {
            if (Estatus != EstatusSolicitud.Pendiente)
            {
                return Result.Failure(AsignacionErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            return Cerrar(EstatusSolicitud.Rechazada, motivo);
        }

        /// <summary>
        /// Anula la asignación por un error de digitación o un documento equivocado.
        /// </summary>
        public Result Cancelar(string motivo)
        {
            if (Estatus is not (EstatusSolicitud.Pendiente or EstatusSolicitud.Aprobada))
            {
                return Result.Failure(AsignacionErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            return Cerrar(EstatusSolicitud.Cancelada, motivo);
        }

        /// <summary>
        /// Registra la entrada del vehículo a la base y finaliza la asignación.
        /// </summary>
        public Result RegistrarRetorno(DateTime fechaRetorno)
        {
            if (Estatus != EstatusSolicitud.Aprobada)
            {
                return Result.Failure(AsignacionErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            if (fechaRetorno < FechaSalida)
            {
                return Result.Failure(AsignacionErrors.FechaRetornoInvalida);
            }

            FechaRetornoReal = fechaRetorno;
            Estatus = EstatusSolicitud.Finalizada;
            return Result.Success();
        }

        private Result Cerrar(EstatusSolicitud estatus, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return Result.Failure(AsignacionErrors.MotivoRequired);
            }

            Estatus = estatus;
            MotivoRechazoCancelacion = motivo;
            return Result.Success();
        }
    }
}
