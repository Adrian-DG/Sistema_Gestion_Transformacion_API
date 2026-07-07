using Domain.Abstraction;
using Domain.Entities.Recursos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Historico
{
    public class Poliza : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public DateOnly FechaExpedicion { get; set; }
        public DateOnly FechaEfectividad { get; set; }
        public DateOnly FechaVencimiento { get; set; }

        public Guid VehiculoId { get; set; }
        public virtual Vehiculo? Vehiculo { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public string DiasRestanteCobertura()
        {
            var actualDate = DateTime.UtcNow;
            int remainingDays = (FechaVencimiento.ToDateTime(TimeOnly.MinValue) - actualDate).Days;
            return remainingDays > 0 ? $"{remainingDays} días restantes" : "Cobertura vencida";
        }

        public string EstatusCobertura()
        {
            var actualDate = DateTime.UtcNow;
            return FechaVencimiento.ToDateTime(TimeOnly.MinValue) > actualDate ? "Vigente" : "Vencida";
        }

        public bool IsCoberturaVigente()
        {
            var actualDate = DateTime.UtcNow;
            return FechaVencimiento.ToDateTime(TimeOnly.MinValue) > actualDate;
        }

        public bool IsCoberturaPorVencer()
        {
            var actualDate = DateTime.UtcNow;
            int remainingDays = (FechaVencimiento.ToDateTime(TimeOnly.MinValue) - actualDate).Days;
            return remainingDays > 0 && remainingDays <= 30;
        }

        public bool IsCoberturaVencida()
        {
            var actualDate = DateTime.UtcNow;
            return FechaVencimiento.ToDateTime(TimeOnly.MinValue) <= actualDate;
        }

    }
}
