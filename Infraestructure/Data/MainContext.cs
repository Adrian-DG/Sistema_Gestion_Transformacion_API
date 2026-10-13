using Domain.Entities.Combustible;
using Domain.Entities.Historico;
using Domain.Entities.Misc;
using Domain.Entities.Organizacion;
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
            builder.Entity<AppUser>(e =>
            {
                e.ToTable("Usuarios", "acceso");
                e.HasIndex(u => u.Identificacion).IsUnique();
                e.HasOne(u => u.Rango).WithMany().HasForeignKey(u => u.RangoId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<AppRole>(e => e.ToTable("Permisos", "acceso"));

            // Miscellaneous entities
            builder.Entity<Aseguradora>(e => e.ToTable("Aseguradoras", "misc"));
            
            builder.Entity<Rango>(e => e.ToTable("Rangos", "misc"));
            builder.Entity<Modelo>(e => e.ToTable("Modelos", "misc"));
            builder.Entity<Marca>(e => e.ToTable("Marcas", "misc"));
            
            builder.Entity<TipoDocumento>(e => e.ToTable("TipoDocumentos", "misc"));
            builder.Entity<TipoVehiculo>(e => e.ToTable("TipoVehiculos", "misc"));
            builder.Entity<TipoOperacion>(e => e.ToTable("TipoOperaciones", "misc"));

            // Organization entities
            builder.Entity<Dependencia>(e =>
            {
                e.ToTable("Dependencias", "organizacion");
                e.HasIndex(d => d.Nombre).IsUnique();
            });

            builder.Entity<Cargo>(e =>
            {
                e.ToTable("Cargos", "organizacion");
                e.HasOne(c => c.Dependencia).WithMany(d => d.Cargos).HasForeignKey(c => c.DependenciaId);
                e.HasOne(c => c.Titular).WithMany().HasForeignKey(c => c.TitularId);
                e.HasIndex(c => new { c.DependenciaId, c.Nombre }).IsUnique();
            });

            builder.Entity<CargoTitularHistorico>(e =>
            {
                e.ToTable("CargoTitulares", "organizacion");
                e.HasOne(h => h.Cargo).WithMany(c => c.HistorialTitulares).HasForeignKey(h => h.CargoId);
            });

            // Historical entities
            builder.Entity<Asignacion>(e =>
            {
                e.ToTable("Asignaciones", "historico", t => t.HasCheckConstraint(
                    "CK_Asignaciones_Beneficiario", BeneficiarioCheck("TipoAsignacion")));
                e.HasOne(a => a.Persona).WithMany().HasForeignKey(a => a.PersonaId);
                e.HasOne(a => a.SolicitantePersona).WithMany().HasForeignKey(a => a.SolicitantePersonaId);
                e.HasIndex(a => new { a.VehiculoId, a.Estatus });
            });

            builder.Entity<Adjunto>(e =>
            {
                e.ToTable("Adjuntos", "historico");
                e.HasIndex(a => new { a.TipoOperacionId, a.IdentificadorOperacion });
            });

            // Fuel entities
            builder.Entity<SolicitudTicket>(e =>
            {
                e.ToTable("SolicitudesTicket", "combustible", t => t.HasCheckConstraint(
                    "CK_SolicitudesTicket_Beneficiario", BeneficiarioCheck("TipoBeneficiario")));
                e.HasOne(s => s.Persona).WithMany().HasForeignKey(s => s.PersonaId);
                e.HasOne(s => s.SolicitantePersona).WithMany().HasForeignKey(s => s.SolicitantePersonaId);
            });

            builder.Entity<TicketCombustible>(e =>
            {
                e.ToTable("Tickets", "combustible", t => t.HasCheckConstraint("CK_Tickets_Galones", "[Galones] > 0"));
                e.Property(t => t.Galones).HasPrecision(10, 2);
                // Un número anulado (error de digitación) puede volver a registrarse.
                e.HasIndex(t => t.Numero).IsUnique().HasFilter($"[Estatus] <> {(int)Domain.Enums.EstatusTicket.Anulado}");
            });

            builder.Entity<MovimientoCombustible>(e =>
            {
                e.ToTable("Movimientos", "combustible", t => t.HasCheckConstraint("CK_Movimientos_Galones", "[Galones] > 0"));
                e.Property(m => m.Galones).HasPrecision(12, 2);
                e.HasOne(m => m.SolicitantePersona).WithMany().HasForeignKey(m => m.SolicitantePersonaId);
                e.HasOne(m => m.ReceptorPersona).WithMany().HasForeignKey(m => m.ReceptorPersonaId);
                e.HasIndex(m => new { m.TipoCombustible, m.Fecha });
            });

            builder.Entity<Poliza>(e =>
            {
                e.ToTable("Polizas", "historico");
                e.HasIndex(p => p.Numero).IsUnique();
            });

            builder.Entity<Persona>(e =>
            {
                e.ToTable("Personas", "historico");
                e.HasIndex(p => p.Identificacion).IsUnique();
            });

            // Resource entities
            builder.Entity<Vehiculo>(e =>
            {
                e.ToTable("Vehiculos", "recursos");
                e.HasIndex(v => v.Placa).IsUnique();
                e.HasIndex(v => v.Chasis).IsUnique();
                e.HasIndex(v => v.Matricula).IsUnique();
            });


            // Los registros se desactivan (Status) en lugar de eliminarse; además varias entidades
            // referencian a Persona más de una vez, lo que SQL Server no admite con borrado en cascada.
            var domainForeignKeys = builder.Model.GetEntityTypes()
                .Where(t => t.ClrType.Assembly == typeof(Asignacion).Assembly)
                .SelectMany(t => t.GetForeignKeys());

            foreach (var foreignKey in domainForeignKeys)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            #endregion

            #region Seed Data
            TipoOperacionSeeder.Seed(builder);
            RoleSeeder.Seed(builder);
            RangoSeeder.Seed(builder);
            var marcas = MarcaSeeder.Seed(builder);
            ModeloSeeder.Seed(builder, marcas);
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

        public DbSet<Dependencia> Dependencias { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<CargoTitularHistorico> CargoTitulares { get; set; }

        public DbSet<SolicitudTicket> SolicitudesTicket { get; set; }
        public DbSet<TicketCombustible> Tickets { get; set; }
        public DbSet<MovimientoCombustible> MovimientosCombustible { get; set; }

        #endregion

        /// <summary>
        /// Exige exactamente un beneficiario: CargoId cuando el tipo es Cargo (1), PersonaId cuando es Persona (2).
        /// </summary>
        private static string BeneficiarioCheck(string tipoColumn) =>
            $"([{tipoColumn}] = 1 AND [CargoId] IS NOT NULL AND [PersonaId] IS NULL) OR " +
            $"([{tipoColumn}] = 2 AND [PersonaId] IS NOT NULL AND [CargoId] IS NULL)";

    }
}
