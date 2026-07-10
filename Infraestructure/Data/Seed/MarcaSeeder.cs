using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infraestructure.Data.Seed
{
    public static class MarcaSeeder
    {
        public static List<Marca> Seed(this ModelBuilder modelBuilder)
        {
            var marcas = new List<Marca>
            {
                new Marca { Id = Guid.NewGuid(), Nombre = "Chevrolet" },
                new Marca { Id = Guid.NewGuid(), Nombre = "Ford" },
                new Marca { Id = Guid.NewGuid(), Nombre = "Jeep" },
                new Marca { Id = Guid.NewGuid(), Nombre = "Mazda" },
                new Marca { Id = Guid.NewGuid(), Nombre = "Mitsubishi" },
                new Marca { Id = Guid.NewGuid(), Nombre = "Nissan" },
                new Marca { Id = Guid.NewGuid(), Nombre = "Toyota" }                
            };

            modelBuilder.Entity<Marca>().HasData(marcas);

            return marcas;
        }
    }
}
