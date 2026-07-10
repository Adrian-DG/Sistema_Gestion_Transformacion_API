using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Infraestructure.Data.Seed
{
    public static class RangoSeeder
    {
        public static void Seed(this ModelBuilder builder)
        {
            var rangos = new List<Rango>
            {
                new Rango { Id = Guid.NewGuid(), Nombre = "Teniente General", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "Mayor General", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "General de Brigada", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "Coronel", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "Teniente Coronel", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "Mayor", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "Capitán", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "Primer Teniente", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "Segundo Teniente", NombreArmada = "" },
                new Rango { Id = Guid.NewGuid(), Nombre = "Subteniente", NombreArmada = "" }
            };

            builder.Entity<Rango>().HasData(rangos);
        }
    }
}
