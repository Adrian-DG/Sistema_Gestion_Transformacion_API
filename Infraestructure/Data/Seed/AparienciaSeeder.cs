using Domain.Entities.Configuracion;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data.Seed
{
    public static class AparienciaSeeder
    {
        private static readonly DateTime FechaSemilla = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void Seed(this ModelBuilder builder)
        {
            var apariencia = Apariencia.Predeterminada();
            apariencia.CreatedAt = FechaSemilla;
            apariencia.UpdatedAt = FechaSemilla;

            builder.Entity<Apariencia>().HasData(apariencia);
        }
    }
}
