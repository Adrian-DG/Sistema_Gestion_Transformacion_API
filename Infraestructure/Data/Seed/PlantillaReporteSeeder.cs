using Domain.Entities.Configuracion;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data.Seed
{
    /// <summary>
    /// Plantilla inicial de los reportes. Sin firmante ni logos: se completan desde la API
    /// (api/plantillas-reporte) con los datos reales de la institución.
    /// </summary>
    public static class PlantillaReporteSeeder
    {
        private static readonly DateTime FechaSemilla = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void Seed(this ModelBuilder builder)
        {
            builder.Entity<PlantillaReporte>().HasData(new PlantillaReporte
            {
                Id = PlantillaReporte.IdInicial,
                Nombre = "Ministerio de Defensa · J-4",
                Descripcion = "Plantilla institucional de la Dirección de Transportación (J-4).",
                EsPredeterminada = true,
                Status = true,
                TextoEncabezado = "República Dominicana\nMinisterio de Defensa\nDirección de Transportación (J-4)",
                NotaPiePagina = "Documento generado por el Sistema de Gestión de Transportación. Uso oficial exclusivo.",
                FirmanteCargo = "Encargado de Transportación",
                CreatedAt = FechaSemilla,
                UpdatedAt = FechaSemilla
            });
        }
    }
}
