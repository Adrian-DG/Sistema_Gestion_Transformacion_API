using Domain.Entities.Combustible;
using Domain.Entities.Configuracion;
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

            // Historical entities
            builder.Entity<Asignacion>(e =>
            {
                // La persona asignada siempre existe; el cargo solo acompaña a las asignaciones al cargo (1).
                e.ToTable("Asignaciones", "historico", t => t.HasCheckConstraint(
                    "CK_Asignaciones_Cargo",
                    "([TipoAsignacion] = 1 AND [CodCargo] IS NOT NULL AND [Cargo] IS NOT NULL) OR " +
                    "([TipoAsignacion] = 2 AND [CodCargo] IS NULL AND [Cargo] IS NULL)"));
                e.HasOne(a => a.Persona).WithMany().HasForeignKey(a => a.PersonaId);
                e.HasOne(a => a.SolicitantePersona).WithMany().HasForeignKey(a => a.SolicitantePersonaId);
                e.HasOne(a => a.ChoferPersona).WithMany().HasForeignKey(a => a.ChoferPersonaId);
                e.HasIndex(a => new { a.VehiculoId, a.Estatus });
                e.HasIndex(a => new { a.ChoferPersonaId, a.Estatus });
                e.Property(a => a.RangoSolicitante).HasMaxLength(100);
                e.Property(a => a.CargoSolicitante).HasMaxLength(200);
                e.Property(a => a.DependenciaSolicitante).HasMaxLength(200);
                e.Property(a => a.Cargo).HasMaxLength(200);
                e.Property(a => a.RangoChofer).HasMaxLength(100);
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
                e.Property(s => s.Cargo).HasMaxLength(200);
                e.Property(s => s.CargoSolicitante).HasMaxLength(200);
                e.Property(s => s.DependenciaSolicitante).HasMaxLength(200);
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
                e.Property(m => m.DependenciaSolicitante).HasMaxLength(200);
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
                e.Property(p => p.Dependencia).HasMaxLength(200);
            });

            // Configuration entities
            builder.Entity<Apariencia>(e =>
            {
                var colores = new[] { "ColorPrimario", "ColorSecundario", "ColorBotonPrimario", "ColorBotonSecundario", "ColorBotonPeligro" };
                e.ToTable("Apariencia", "configuracion", t =>
                {
                    foreach (var color in colores)
                    {
                        t.HasCheckConstraint($"CK_Apariencia_{color}", $"[{color}] LIKE '#[0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F]'");
                    }
                });
                foreach (var color in colores)
                {
                    e.Property<string>(color).HasMaxLength(7).IsUnicode(false);
                }
                e.Property(a => a.NombreSistema).HasMaxLength(150);
                e.Property(a => a.NombreCorto).HasMaxLength(30);
                e.Property(a => a.NombreInstitucion).HasMaxLength(200);
                e.Property(a => a.LogoContentType).HasMaxLength(50);
                e.Property(a => a.LogoNombreArchivo).HasMaxLength(260);
            });

            builder.Entity<PlantillaReporte>(e =>
            {
                e.ToTable("PlantillasReporte", "configuracion");
                e.HasIndex(p => p.Nombre).IsUnique();
                // Una sola plantilla predeterminada.
                e.HasIndex(p => p.EsPredeterminada).IsUnique().HasFilter("[EsPredeterminada] = 1");
                e.Property(p => p.Nombre).HasMaxLength(100);
                e.Property(p => p.Descripcion).HasMaxLength(300);
                e.Property(p => p.TextoEncabezado).HasMaxLength(1000);
                e.Property(p => p.NotaPiePagina).HasMaxLength(1000);
                e.Property(p => p.FirmanteNombre).HasMaxLength(150);
                e.Property(p => p.FirmanteRango).HasMaxLength(100);
                e.Property(p => p.FirmanteProfesion).HasMaxLength(100);
                e.Property(p => p.FirmanteCargo).HasMaxLength(150);
            });

            builder.Entity<PlantillaReporteLogo>(e =>
            {
                e.ToTable("PlantillaReporteLogos", "configuracion", t => t.HasCheckConstraint("CK_PlantillaReporteLogos_Posicion", "[Posicion] IN (1, 2, 3)"));
                e.HasIndex(l => new { l.PlantillaReporteId, l.Posicion }).IsUnique();
                e.HasOne(l => l.PlantillaReporte).WithMany(p => p.Logos).HasForeignKey(l => l.PlantillaReporteId);
                e.Property(l => l.ContentType).HasMaxLength(50);
                e.Property(l => l.NombreArchivo).HasMaxLength(260);
            });

            builder.Entity<PlantillaReporteTitulo>(e =>
            {
                e.ToTable("PlantillaReporteTitulos", "configuracion");
                e.HasIndex(t => new { t.PlantillaReporteId, t.TipoReporte }).IsUnique();
                e.HasOne(t => t.PlantillaReporte).WithMany(p => p.Titulos).HasForeignKey(t => t.PlantillaReporteId);
                e.Property(t => t.Titulo).HasMaxLength(200);
                e.Property(t => t.Subtitulo).HasMaxLength(300);
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
            AparienciaSeeder.Seed(builder);
            PlantillaReporteSeeder.Seed(builder);
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

        public DbSet<SolicitudTicket> SolicitudesTicket { get; set; }
        public DbSet<TicketCombustible> Tickets { get; set; }
        public DbSet<MovimientoCombustible> MovimientosCombustible { get; set; }

        public DbSet<Apariencia> Apariencia { get; set; }
        public DbSet<PlantillaReporte> PlantillasReporte { get; set; }
        public DbSet<PlantillaReporteLogo> PlantillaReporteLogos { get; set; }
        public DbSet<PlantillaReporteTitulo> PlantillaReporteTitulos { get; set; }

        #endregion

        /// <summary>
        /// Exige exactamente un beneficiario de tickets: el cargo de SIPFFAA cuando el tipo es Cargo (1), PersonaId cuando es Persona (2).
        /// </summary>
        private static string BeneficiarioCheck(string tipoColumn) =>
            $"([{tipoColumn}] = 1 AND [CodCargo] IS NOT NULL AND [Cargo] IS NOT NULL AND [PersonaId] IS NULL) OR " +
            $"([{tipoColumn}] = 2 AND [PersonaId] IS NOT NULL AND [CodCargo] IS NULL AND [Cargo] IS NULL)";

    }
}
