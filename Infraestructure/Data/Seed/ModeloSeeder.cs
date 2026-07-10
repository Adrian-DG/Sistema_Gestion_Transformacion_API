using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infraestructure.Data.Seed
{
    public static class ModeloSeeder
    {
        public static void Seed(this ModelBuilder modelBuilder, List<Marca> marcas)
        {
            var modelos = new List<Modelo>
            {
                new Modelo { Id = Guid.NewGuid(), Nombre = "Colorado", MarcaId = marcas.First(m => m.Nombre == "Chevrolet").Id },
                new Modelo { Id = Guid.NewGuid(), Nombre = "Ranger", MarcaId = marcas.First(m => m.Nombre == "Ford").Id },
                new Modelo { Id = Guid.NewGuid(), Nombre = "Frontier", MarcaId = marcas.First(m => m.Nombre == "Nissan").Id },
                new Modelo { Id = Guid.NewGuid(), Nombre = "Wrangler", MarcaId = marcas.First(m => m.Nombre == "Jeep").Id },
                new Modelo { Id = Guid.NewGuid(), Nombre = "BT-50", MarcaId = marcas.First(m => m.Nombre == "Mazda").Id },
                new Modelo { Id = Guid.NewGuid(), Nombre = "L200", MarcaId = marcas.First(m => m.Nombre == "Mitsubishi").Id },
                new Modelo { Id = Guid.NewGuid(), Nombre = "Hilux", MarcaId = marcas.First(m => m.Nombre == "Toyota").Id },
            };
                
            modelBuilder.Entity<Modelo>().HasData(modelos);            
        }
    }
}
