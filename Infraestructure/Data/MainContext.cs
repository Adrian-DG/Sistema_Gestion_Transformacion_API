using Domain.Entities.Combustible;
using Domain.Entities.Configuracion;
using Domain.Entities.Historico;
using Domain.Entities.Misc;
using Domain.Entities.Recursos;
using Domain.Entities.Taller;
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
                // Los anulados quedan fuera del índice: los tickets pre-impresos anulados podían volver a registrarse.
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

            // Workshop entities
            builder.Entity<Proveedor>(e =>
            {
                e.ToTable("Proveedores", "taller");
                e.HasIndex(p => p.Rnc).IsUnique().HasFilter("[Rnc] IS NOT NULL");
                e.Property(p => p.Nombre).HasMaxLength(200);
                e.Property(p => p.Rnc).HasMaxLength(20);
                e.Property(p => p.Telefono).HasMaxLength(20);
                e.Property(p => p.Correo).HasMaxLength(150);
                e.Property(p => p.Direccion).HasMaxLength(300);
                e.Property(p => p.Contacto).HasMaxLength(150);
            });

            builder.Entity<Pieza>(e =>
            {
                e.ToTable("Piezas", "taller", t => t.HasCheckConstraint("CK_Piezas_ExistenciaMinima", "[ExistenciaMinima] >= 0"));
                e.HasIndex(p => p.Codigo).IsUnique();
                e.Property(p => p.Codigo).HasMaxLength(30);
                e.HasIndex(p => p.CodigoBarras).IsUnique().HasFilter("[CodigoBarras] IS NOT NULL");
                e.Property(p => p.CodigoBarras).HasMaxLength(64).IsUnicode(false);
                e.Property(p => p.Nombre).HasMaxLength(200);
                e.Property(p => p.Descripcion).HasMaxLength(500);
                e.Property(p => p.UnidadMedida).HasMaxLength(30);
                e.Property(p => p.ExistenciaMinima).HasPrecision(12, 2);
            });

            builder.Entity<EntradaPieza>(e =>
            {
                e.ToTable("EntradasPieza", "taller");
                e.Property(x => x.NumeroFactura).HasMaxLength(50);
                e.Property(x => x.Observacion).HasMaxLength(500);
                e.HasIndex(x => x.Fecha);
            });

            builder.Entity<MovimientoPieza>(e =>
            {
                e.ToTable("MovimientosPieza", "taller", t =>
                {
                    t.HasCheckConstraint("CK_MovimientosPieza_Cantidad", "[Cantidad] > 0");
                    t.HasCheckConstraint("CK_MovimientosPieza_Costo", "[CostoUnitario] >= 0");
                });
                e.Property(m => m.Cantidad).HasPrecision(12, 2);
                e.Property(m => m.CostoUnitario).HasPrecision(14, 2);
                e.Property(m => m.NumeroFactura).HasMaxLength(50);
                e.HasOne(m => m.EntradaPieza).WithMany(x => x.Lineas).HasForeignKey(m => m.EntradaPiezaId);
                e.HasOne(m => m.SolicitudTaller).WithMany(s => s.Movimientos).HasForeignKey(m => m.SolicitudTallerId);
                e.HasIndex(m => new { m.PiezaId, m.Fecha });
            });

            builder.Entity<Mecanico>(e =>
            {
                e.ToTable("Mecanicos", "taller");
                e.HasIndex(m => m.PersonaId).IsUnique();
                e.Property(m => m.Especialidad).HasMaxLength(100);
            });

            builder.Entity<SolicitudTaller>(e =>
            {
                // Externa ⇔ con taller autorizado.
                e.ToTable("Solicitudes", "taller", t => t.HasCheckConstraint("CK_Solicitudes_TallerExterno",
                    $"([Lugar] = {(int)Domain.Enums.LugarMantenimiento.Externo} AND [TallerExternoId] IS NOT NULL) OR " +
                    $"([Lugar] = {(int)Domain.Enums.LugarMantenimiento.Interno} AND [TallerExternoId] IS NULL)"));
                e.HasIndex(s => s.Numero).IsUnique();
                e.HasIndex(s => new { s.VehiculoId, s.Estatus });
                e.HasIndex(s => s.FechaSolicitud);
                e.Property(s => s.Numero).HasMaxLength(20);
                e.Property(s => s.Motivo).HasMaxLength(1000);
                e.Property(s => s.Diagnostico).HasMaxLength(2000);
                e.Property(s => s.TrabajoRealizado).HasMaxLength(2000);
                e.Property(s => s.MotivoRechazoCancelacion).HasMaxLength(500);
                e.Property(s => s.CostoServicio).HasPrecision(14, 2);
                e.Property(s => s.NumeroFacturaServicio).HasMaxLength(50);
                e.HasOne(s => s.TallerExterno).WithMany().HasForeignKey(s => s.TallerExternoId);
                // Una sola solicitud abierta (Pendiente o EnTaller) por vehículo.
                e.HasIndex(s => s.VehiculoId).IsUnique()
                    .HasFilter($"[Estatus] IN ({(int)Domain.Enums.EstatusSolicitudTaller.Pendiente}, {(int)Domain.Enums.EstatusSolicitudTaller.EnTaller})")
                    .HasDatabaseName("IX_Solicitudes_VehiculoId_Abierta");
            });

            builder.Entity<ReactivacionVehiculo>(e =>
            {
                e.ToTable("ReactivacionesVehiculo", "taller");
                e.Property(r => r.Motivo).HasMaxLength(1000);
                e.HasIndex(r => r.VehiculoId);
            });

            // Configuration entities
            builder.Entity<Secuencia>(e =>
            {
                e.ToTable("Secuencias", "configuracion");
                e.HasKey(s => new { s.Codigo, s.Anio });
                e.Property(s => s.Codigo).HasMaxLength(50);
            });

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
                e.HasIndex(a => a.Nombre).IsUnique();
                // Una sola apariencia activa.
                e.HasIndex(a => a.EsActiva).IsUnique().HasFilter("[EsActiva] = 1");
                e.Property(a => a.Nombre).HasMaxLength(100);
                e.Property(a => a.Descripcion).HasMaxLength(300);
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
                e.HasIndex(v => v.Ficha).IsUnique().HasFilter("[Ficha] IS NOT NULL");
                e.Property(v => v.Ficha).HasMaxLength(20);
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
            MarcaSeeder.Seed(builder);
            ModeloSeeder.Seed(builder);
            CatalogoVehiculoSeeder.Seed(builder);
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

        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Pieza> Piezas { get; set; }
        public DbSet<EntradaPieza> EntradasPieza { get; set; }
        public DbSet<MovimientoPieza> MovimientosPieza { get; set; }
        public DbSet<Mecanico> Mecanicos { get; set; }
        public DbSet<SolicitudTaller> SolicitudesTaller { get; set; }
        public DbSet<ReactivacionVehiculo> ReactivacionesVehiculo { get; set; }

        public DbSet<Secuencia> Secuencias { get; set; }
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
