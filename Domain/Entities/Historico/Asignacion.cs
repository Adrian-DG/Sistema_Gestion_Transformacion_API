using Domain.Abstraction;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Recursos;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Historico
{
    /// <summary>
    /// Solicitud y asignación de un vehículo a una persona (a título personal o en nombre de un cargo),
    /// con el chofer que lo conduce.
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

        // Los cargos, dependencias y rangos se guardan con el valor vigente al registrar la solicitud,
        // para que el histórico no cambie si SIPFFAA o la persona se actualizan.

        // Quién lo solicita y para qué (la cédula es la Identificacion de la persona)
        public Guid SolicitantePersonaId { get; set; }
        public virtual Persona? SolicitantePersona { get; set; }
        public string? RangoSolicitante { get; set; }
        /// <summary>CodCargo de SIPFFAA; nulo si el cargo no está en SIPFFAA.</summary>
        public int? CodCargoSolicitante { get; set; }
        public required string CargoSolicitante { get; set; }
        /// <summary>CodDepartamento de SIPFFAA; nulo si la dependencia no está en SIPFFAA.</summary>
        public int? CodDependenciaSolicitante { get; set; }
        public required string DependenciaSolicitante { get; set; }

        /// <summary>
        /// Persona a quien se asigna el vehículo; puede ser el mismo solicitante. En asignaciones al cargo
        /// (<see cref="TipoBeneficiario.Cargo"/>) es quien lo recibe en nombre del cargo indicado.
        /// </summary>
        public TipoBeneficiario TipoAsignacion { get; set; }
        public Guid PersonaId { get; set; }
        public virtual Persona? Persona { get; set; }
        public int? CodCargo { get; set; }
        public string? Cargo { get; set; }

        // Chofer que conduce el vehículo
        public Guid ChoferPersonaId { get; set; }
        public virtual Persona? ChoferPersona { get; set; }
        public string? RangoChofer { get; set; }

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
