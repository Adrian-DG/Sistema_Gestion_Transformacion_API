using Domain.Constants;
using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data.Seed
{
    public static class TipoOperacionSeeder
    {
        public static void Seed(this ModelBuilder builder)
        {
            builder.Entity<TipoOperacion>().HasData(
                new TipoOperacion { Id = TiposOperacion.Vehiculo, Nombre = "Vehículo", Status = true },
                new TipoOperacion { Id = TiposOperacion.Asignacion, Nombre = "Asignación de vehículo", Status = true },
                new TipoOperacion { Id = TiposOperacion.SolicitudTicket, Nombre = "Solicitud de tickets de combustible", Status = true },
                new TipoOperacion { Id = TiposOperacion.MovimientoCombustible, Nombre = "Movimiento de combustible", Status = true });
        }
    }
}
