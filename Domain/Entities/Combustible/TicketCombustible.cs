using Domain.Abstraction;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Enums;

namespace Domain.Entities.Combustible
{
    /// <summary>
    /// Ticket de combustible (actualmente pre-impreso con su número y galones).
    /// Se consume completo; quien lo recibe puede cederlo, solo se controla
    /// a quién se entregó y cuándo fue utilizado.
    /// </summary>
    public class TicketCombustible : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public required string Numero { get; set; }
        public TipoCombustible TipoCombustible { get; set; }
        public decimal Galones { get; set; }
        public EstatusTicket Estatus { get; set; }

        public Guid SolicitudTicketId { get; set; }
        public virtual SolicitudTicket? SolicitudTicket { get; set; }

        public DateTime? FechaUso { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public Result Utilizar(DateTime fechaUso)
        {
            if (Estatus != EstatusTicket.Disponible)
            {
                return Result.Failure(TicketCombustibleErrors.NoDisponible(Numero, Estatus.ToString()));
            }

            Estatus = EstatusTicket.Utilizado;
            FechaUso = fechaUso;
            return Result.Success();
        }
    }
}
