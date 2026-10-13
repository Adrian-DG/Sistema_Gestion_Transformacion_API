using Domain.Abstraction;
using Domain.Entities.Historico;

namespace Domain.Entities.Organizacion
{
    /// <summary>
    /// Puesto dentro de una dependencia (Director, Subdirector, Ayudante...).
    /// Los vehículos y tickets se pueden asignar al cargo; el titular puede cambiar
    /// sin cerrar las asignaciones, y el cambio queda en <see cref="HistorialTitulares"/>.
    /// </summary>
    public class Cargo : NamedEntityMetadata, IAuditableEntityMetadata
    {
        public Guid DependenciaId { get; set; }
        public virtual Dependencia? Dependencia { get; set; }

        public Guid? TitularId { get; set; }
        public virtual Persona? Titular { get; set; }

        public virtual ICollection<CargoTitularHistorico> HistorialTitulares { get; set; } = [];

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        /// <summary>
        /// Cierra el período del titular vigente y abre uno nuevo.
        /// Requiere que <see cref="HistorialTitulares"/> esté cargado.
        /// </summary>
        public void CambiarTitular(Guid? nuevoTitularId, DateTime fechaCambio)
        {
            if (TitularId == nuevoTitularId) return;

            var vigente = HistorialTitulares.FirstOrDefault(h => h.FechaHasta is null);
            if (vigente is not null)
            {
                vigente.FechaHasta = fechaCambio;
            }

            TitularId = nuevoTitularId;

            if (nuevoTitularId.HasValue)
            {
                HistorialTitulares.Add(new CargoTitularHistorico
                {
                    CargoId = Id,
                    PersonaId = nuevoTitularId.Value,
                    FechaDesde = fechaCambio
                });
            }
        }
    }
}
