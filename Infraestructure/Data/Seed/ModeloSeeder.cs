using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data.Seed
{
    /// <summary>
    /// Modelos iniciales, uno por marca de <see cref="MarcaSeeder"/>. Ids fijos y Status activo.
    /// </summary>
    public static class ModeloSeeder
    {
        public static readonly Guid Colorado = new("3b7d0f52-6c4e-4081-9f23-7e0000000001");
        public static readonly Guid Ranger = new("3b7d0f52-6c4e-4081-9f23-7e0000000002");
        public static readonly Guid Frontier = new("3b7d0f52-6c4e-4081-9f23-7e0000000003");
        public static readonly Guid Wrangler = new("3b7d0f52-6c4e-4081-9f23-7e0000000004");
        public static readonly Guid BT50 = new("3b7d0f52-6c4e-4081-9f23-7e0000000005");
        public static readonly Guid L200 = new("3b7d0f52-6c4e-4081-9f23-7e0000000006");
        public static readonly Guid Hilux = new("3b7d0f52-6c4e-4081-9f23-7e0000000007");

        public static void Seed(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Modelo>().HasData(
                new Modelo { Id = Colorado, Nombre = "Colorado", MarcaId = MarcaSeeder.Chevrolet, Status = true },
                new Modelo { Id = Ranger, Nombre = "Ranger", MarcaId = MarcaSeeder.Ford, Status = true },
                new Modelo { Id = Frontier, Nombre = "Frontier", MarcaId = MarcaSeeder.Nissan, Status = true },
                new Modelo { Id = Wrangler, Nombre = "Wrangler", MarcaId = MarcaSeeder.Jeep, Status = true },
                new Modelo { Id = BT50, Nombre = "BT-50", MarcaId = MarcaSeeder.Mazda, Status = true },
                new Modelo { Id = L200, Nombre = "L200", MarcaId = MarcaSeeder.Mitsubishi, Status = true },
                new Modelo { Id = Hilux, Nombre = "Hilux", MarcaId = MarcaSeeder.Toyota, Status = true });
        }
    }
}
