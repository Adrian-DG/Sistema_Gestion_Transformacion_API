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
                // Oficiales (NombreArmada: equivalente en la Armada)
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000001"), Nombre = "Teniente General", NombreArmada = "Almirante", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000002"), Nombre = "Mayor General", NombreArmada = "Vicealmirante", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000003"), Nombre = "General de Brigada", NombreArmada = "Contralmirante", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000004"), Nombre = "Coronel", NombreArmada = "Capitán de Navío", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000005"), Nombre = "Teniente Coronel", NombreArmada = "Capitán de Fragata", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000006"), Nombre = "Mayor", NombreArmada = "Capitán de Corbeta", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000007"), Nombre = "Capitán", NombreArmada = "Teniente de Navío", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000008"), Nombre = "Primer Teniente", NombreArmada = "Teniente de Fragata", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000009"), Nombre = "Segundo Teniente", NombreArmada = "Teniente de Corbeta", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000010"), Nombre = "Subteniente", NombreArmada = "Subteniente", Status = true },

                // Alistados (NombreArmada: equivalente en la Armada)
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000011"), Nombre = "Sargento Mayor", NombreArmada = "Sargento Mayor", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000012"), Nombre = "Sargento", NombreArmada = "Sargento", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000013"), Nombre = "Cabo", NombreArmada = "Cabo", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000014"), Nombre = "Raso", NombreArmada = "Marinero", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000015"), Nombre = "Conscripto", NombreArmada = "Grumete", Status = true },

                // Asimilados: personal civil con asimilación militar. Los nombres siguen los de SIPFFAA para que la
                // importación de personas los asocie (compara sin distinguir mayúsculas ni tildes).
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000016"), Nombre = "Asimilado Militar Categoría I, Título de Doctorado", NombreArmada = "Asimilado Militar Categoría I, Título de Doctorado", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000017"), Nombre = "Asimilado Militar Categoría II, Título de Maestría", NombreArmada = "Asimilado Militar Categoría II, Título de Maestría", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000018"), Nombre = "Asimilado Militar Categoría III, Título de Especialidad", NombreArmada = "Asimilado Militar Categoría III, Título de Especialidad", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000019"), Nombre = "Asimilado Militar Categoría IV, Título de Grado", NombreArmada = "Asimilado Militar Categoría IV, Título de Grado", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000020"), Nombre = "Asimilado Militar Categoría V, Técnico Superior", NombreArmada = "Asimilado Militar Categoría V, Técnico Superior", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000021"), Nombre = "Asimilado Militar Categoría VI, Técnico Especialista", NombreArmada = "Asimilado Militar Categoría VI, Técnico Especialista", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000022"), Nombre = "Asimilado Militar Categoría VII, Técnico", NombreArmada = "Asimilado Militar Categoría VII, Técnico", Status = true },
                new Rango { Id = new("1d3f7b2a-6c51-4e08-9a7d-4b0000000023"), Nombre = "Asimilado Militar Categoría VIII, Conserjería", NombreArmada = "Asimilado Militar Categoría VIII, Conserjería", Status = true }
            };

            builder.Entity<Rango>().HasData(rangos);
        }
    }
}
