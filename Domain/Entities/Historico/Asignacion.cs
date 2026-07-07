using Domain.Abstraction;
using Domain.Entities.Recursos;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities.Historico
{
    public class Asignacion : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public EstatusAsignacion Estatus { get; set; }
        public DateOnly FechaEfectividad { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Motivo { get; set; }

        public Guid PersonaId { get; set; }
        public virtual Persona? Persona { get; set; }
        public Guid VehiculoId { get; set; }
        public virtual Vehiculo? Vehiculo { get; set; }
        public virtual ICollection<Adjunto>? Adjuntos { get; set; }

        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }


        public void SetAsignacion(Guid vehiculoId, Guid personaId)
        {
            Estatus = EstatusAsignacion.PendienteConfirmacion;
            VehiculoId = vehiculoId;
            PersonaId = personaId;
        }

        public void ConfirmAsignacion(int userId)
        {
            Estatus = EstatusAsignacion.Confirmado;
        }

        public void CancelAsignacion(string motivo, int userId)
        {
            Estatus = EstatusAsignacion.Cancelado;
        }

    }
}
