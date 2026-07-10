using Domain.Entities.Historico;
using Domain.Entities.Misc;
using Domain.Entities.Recursos;
using Infraestructure.Data.Seed;
using Infraestructure.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infraestructure.Data
{
    public class MainContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        public MainContext(DbContextOptions<MainContext> options) : base(options)
        {
        }

        protected MainContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(MainContext).Assembly);

            #region Entity Configurations

            // Identity entities
            builder.Entity<AppUser>(e => e.ToTable("Usuarios", "acceso"));
            builder.Entity<AppRole>(e => e.ToTable("Permisos", "acceso"));

            // Miscellaneous entities
            builder.Entity<Aseguradora>(e => e.ToTable("Aseguradoras", "misc"));
            builder.Entity<Rango>(e => e.ToTable("Rangos", "misc"));
            builder.Entity<Modelo>(e => e.ToTable("Modelos", "misc"));
            builder.Entity<Marca>(e => e.ToTable("Marcas", "misc"));
            builder.Entity<TipoDocumento>(e => e.ToTable("TipoDocumentos", "misc"));
            builder.Entity<TipoVehiculo>(e => e.ToTable("TipoVehiculos", "misc"));
            builder.Entity<TipoOperacion>(e => e.ToTable("TipoOperaciones", "misc"));

            // Historical entities
            builder.Entity<Asignacion>(e => e.ToTable("Asignaciones", "historico"));
            builder.Entity<Adjunto>(e => e.ToTable("Adjuntos", "historico"));
            builder.Entity<Poliza>(e => e.ToTable("Polizas", "historico"));
            builder.Entity<Persona>(e => e.ToTable("Personas", "historico"));

            // Resource entities
            builder.Entity<Vehiculo>(e => e.ToTable("Vehiculos", "recursos"));


            #endregion

            #region Seed Data
            RangoSeeder.Seed(builder);
            #endregion
        }


        #region Tables
        public DbSet<Aseguradora> Aseguradoras { get; set; }
        public DbSet<Rango> Rangos { get; set; }
        public DbSet<Modelo> Modelos { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<TipoDocumento> TipoDocumentos { get; set; }
        public DbSet<TipoVehiculo> TipoVehiculos { get; set; }
        public DbSet<TipoOperacion> TipoOperaciones { get; set; }

        public DbSet<Asignacion> Asignaciones { get; set; }
        public DbSet<Adjunto> Adjuntos { get; set; }
        public DbSet<Poliza> Polizas { get; set; }
        public DbSet<Persona> Personas { get; set; }

        public DbSet<Vehiculo> Vehiculos { get; set; }

        #endregion

    }
}
