using Domain.Abstraction;
using Domain.Common;
using Domain.Common.Errors;
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

        [DataType(DataType.MultilineText)]
        public string? MotivoCancelacion { get; set; }

        public Guid PersonaId { get; set; }
        public virtual Persona? Persona { get; set; }
        public Guid VehiculoId { get; set; }
        public virtual Vehiculo? Vehiculo { get; set; }
        public virtual ICollection<Adjunto>? Adjuntos { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }


        public void SetAsignacion(Guid vehiculoId, Guid personaId)
        {
            Estatus = EstatusAsignacion.PendienteConfirmacion;
            VehiculoId = vehiculoId;
            PersonaId = personaId;
        }

        public Result ConfirmAsignacion()
        {
            if (Estatus != EstatusAsignacion.PendienteConfirmacion)
            {
                return Result.Failure(AsignacionErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            Estatus = EstatusAsignacion.Confirmado;
            return Result.Success();
        }

        /// <summary>
        /// Cancela la asignación y libera el vehículo para que pueda volver a asignarse.
        /// </summary>
        public Result CancelAsignacion(string motivo, Vehiculo vehiculo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return Result.Failure(AsignacionErrors.MotivoCancelacionRequired);
            }

            if (Estatus == EstatusAsignacion.Cancelado)
            {
                return Result.Failure(AsignacionErrors.InvalidStatus(Id, Estatus.ToString()));
            }

            Estatus = EstatusAsignacion.Cancelado;
            MotivoCancelacion = motivo;
            vehiculo.Disponible = true;
            return Result.Success();
        }

    }
}
