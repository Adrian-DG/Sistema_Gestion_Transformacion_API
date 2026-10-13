using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Infraestructure.Data.Seed
{
    /// <summary>
    /// Rangos iniciales. Ids fijos para que el modelo no cambie en cada compilación; Status activo
    /// porque los catálogos solo devuelven registros activos.
    /// </summary>
    public static class RangoSeeder
    {
        public static void Seed(this ModelBuilder builder)
        {
            var rangos = new List<Rango>
            {
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000001"), Nombre = "Teniente General", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000002"), Nombre = "Mayor General", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000003"), Nombre = "General de Brigada", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000004"), Nombre = "Coronel", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000005"), Nombre = "Teniente Coronel", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000006"), Nombre = "Mayor", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000007"), Nombre = "Capitán", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000008"), Nombre = "Primer Teniente", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000009"), Nombre = "Segundo Teniente", NombreArmada = "", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000010"), Nombre = "Subteniente", NombreArmada = "", Status = true }
            };

            builder.Entity<Rango>().HasData(rangos);
        }
    }
}
