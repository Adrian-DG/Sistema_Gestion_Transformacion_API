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
                new Rango { Nombre = "Teniente General", NombreArmada = "" },
                new Rango { Nombre = "Mayor General", NombreArmada = "" },
                new Rango { Nombre = "General de Brigada", NombreArmada = "" },
                new Rango { Nombre = "Coronel", NombreArmada = "" },
                new Rango { Nombre = "Teniente Coronel", NombreArmada = "" },
                new Rango { Nombre = "Mayor", NombreArmada = "" },
                new Rango { Nombre = "Capitán", NombreArmada = "" },
                new Rango { Nombre = "Primer Teniente", NombreArmada = "" },
                new Rango { Nombre = "Segundo Teniente", NombreArmada = "" },
                new Rango { Nombre = "Subteniente", NombreArmada = "" }
            };

            builder.Entity<Rango>().HasData(rangos);
        }
    }
}
