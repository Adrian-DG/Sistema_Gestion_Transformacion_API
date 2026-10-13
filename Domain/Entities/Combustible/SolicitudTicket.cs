using Domain.Abstraction;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Entities.Organizacion;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Combustible
{
    /// <summary>
    /// Solicitud de tickets de combustible. Registra quién la pide, a quién (cargo o persona)
    /// se le entregan los tickets y los tickets entregados con su numeración.
    /// </summary>
    public class SolicitudTicket : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public EstatusSolicitud Estatus { get; set; }

        // A quién se le entregan los tickets
        public TipoBeneficiario TipoBeneficiario { get; set; }
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

        public virtual ICollection<TicketCombustible> Tickets { get; set; } = [];

        public DateTime? FechaAprobacion { get; set; }
        public Guid? AprobadoPor { get; set; }

        [DataType(DataType.MultilineText)]
        public string? MotivoRechazoCancelacion { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        /// <summary>Requiere que <see cref="Tickets"/> esté cargado.</summary>
        public Result Aprobar(Guid usuarioId, DateTime fecha)
        {
            if (Estatus != EstatusSolicitud.Pendiente)
            {
                return Result.Failure(SolicitudTicketErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            Estatus = EstatusSolicitud.Aprobada;
            FechaAprobacion = fecha;
            AprobadoPor = usuarioId;

            foreach (var ticket in Tickets)
            {
                ticket.Estatus = EstatusTicket.Disponible;
            }

            return Result.Success();
        }

        /// <summary>Requiere que <see cref="Tickets"/> esté cargado.</summary>
        public Result Rechazar(string motivo)
        {
            if (Estatus != EstatusSolicitud.Pendiente)
            {
                return Result.Failure(SolicitudTicketErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            return Cerrar(EstatusSolicitud.Rechazada, motivo);
        }

        /// <summary>
        /// Anula la solicitud por un error de digitación o un documento equivocado.
        /// No se permite si alguno de sus tickets ya fue utilizado. Requiere que <see cref="Tickets"/> esté cargado.
        /// </summary>
        public Result Cancelar(string motivo)
        {
            if (Estatus is not (EstatusSolicitud.Pendiente or EstatusSolicitud.Aprobada))
            {
                return Result.Failure(SolicitudTicketErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            if (Tickets.Any(t => t.Estatus == EstatusTicket.Utilizado))
            {
                return Result.Failure(SolicitudTicketErrors.TieneTicketsUtilizados(Id));
            }

            return Cerrar(EstatusSolicitud.Cancelada, motivo);
        }

        private Result Cerrar(EstatusSolicitud estatus, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return Result.Failure(SolicitudTicketErrors.MotivoRequired);
            }

            Estatus = estatus;
            MotivoRechazoCancelacion = motivo;

            foreach (var ticket in Tickets)
            {
                ticket.Estatus = EstatusTicket.Anulado;
            }

            return Result.Success();
        }
    }
}
