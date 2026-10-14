using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data.Seed
{
    /// <summary>
    /// Marcas iniciales. Ids fijos para que el modelo no cambie en cada compilación; Status activo
    /// porque los catálogos solo devuelven registros activos.
    /// </summary>
    public static class MarcaSeeder
    {
        public static readonly Guid Chevrolet = new("2a6c9e41-5b3d-4f70-8e12-6d0000000001");
        public static readonly Guid Ford = new("2a6c9e41-5b3d-4f70-8e12-6d0000000002");
        public static readonly Guid Jeep = new("2a6c9e41-5b3d-4f70-8e12-6d0000000003");
        public static readonly Guid Mazda = new("2a6c9e41-5b3d-4f70-8e12-6d0000000004");
        public static readonly Guid Mitsubishi = new("2a6c9e41-5b3d-4f70-8e12-6d0000000005");
        public static readonly Guid Nissan = new("2a6c9e41-5b3d-4f70-8e12-6d0000000006");
        public static readonly Guid Toyota = new("2a6c9e41-5b3d-4f70-8e12-6d0000000007");

        public static void Seed(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Marca>().HasData(
                new Marca { Id = Chevrolet, Nombre = "Chevrolet", Status = true },
                new Marca { Id = Ford, Nombre = "Ford", Status = true },
                new Marca { Id = Jeep, Nombre = "Jeep", Status = true },
                new Marca { Id = Mazda, Nombre = "Mazda", Status = true },
                new Marca { Id = Mitsubishi, Nombre = "Mitsubishi", Status = true },
                new Marca { Id = Nissan, Nombre = "Nissan", Status = true },
                new Marca { Id = Toyota, Nombre = "Toyota", Status = true });
        }
    }
}
