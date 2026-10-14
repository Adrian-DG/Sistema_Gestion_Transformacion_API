using Application.Security;
using Domain.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Data.Seed.Demo
{
    /// <summary>
    /// Datos de ejemplo para desarrollo y demostraciones: personas, vehículos, asignaciones, pólizas, taller y
    /// usuarios con distintos permisos. No usa HasData para que estos datos nunca lleguen a las migraciones
    /// (y por tanto a producción); se ejecuta al iniciar solo si <c>DatosDemo:Habilitado</c> es true.
    /// Las cédulas usan la serie 999, que no corresponde a cédulas reales.
    /// </summary>
    public static class DatosDemoSeeder
    {
        public const string Seccion = "DatosDemo";

        public static async Task SeedDatosDemoAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            await using var scope = services.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            var configuration = provider.GetRequiredService<IConfiguration>();
            if (!bool.TryParse(configuration[$"{Seccion}:Habilitado"], out var habilitado) || !habilitado) return;

            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatosDemoSeeder));
            var context = provider.GetRequiredService<MainContext>();

            if (!await context.Database.EstaListaParaSeedAsync(cancellationToken))
            {
                logger.LogWarning("La base de datos no existe o tiene migraciones pendientes; no se cargaron los datos de ejemplo.");
                return;
            }

            if (await context.Vehiculos.AnyAsync(cancellationToken) || await context.Personas.AnyAsync(cancellationToken))
            {
                logger.LogInformation("Ya hay vehículos o personas registrados; se omiten los datos de ejemplo de operaciones.");
            }
            else
            {
                var demo = new DatosDemo(context, await AdministradorIdAsync(context, cancellationToken));

                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                var personas = await PersonaDemoSeeder.SeedAsync(demo, cancellationToken);
                var vehiculos = VehiculoDemoSeeder.Seed(demo);
                var asignaciones = AsignacionDemoSeeder.Seed(demo, personas, vehiculos);
                var polizas = PolizaDemoSeeder.Seed(demo, vehiculos);
                var ordenesTaller = TallerDemoSeeder.Seed(demo, personas, vehiculos);

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                context.ChangeTracker.Clear();

                logger.LogInformation(
                    "Datos de ejemplo cargados: {Personas} personas, {Vehiculos} vehículos, {Asignaciones} asignaciones, {Polizas} pólizas y {Ordenes} órdenes de taller.",
                    personas.Todas.Count, vehiculos.Todos.Count, asignaciones, polizas, ordenesTaller);
            }

            await UsuarioDemoSeeder.SeedAsync(provider, configuration[$"{Seccion}:Password"], logger, cancellationToken);
        }

        /// <summary>Los datos se registran a nombre del administrador, si ya existe.</summary>
        private static Task<Guid> AdministradorIdAsync(MainContext context, CancellationToken cancellationToken) =>
            (from ur in context.UserRoles
             join r in context.Roles on ur.RoleId equals r.Id
             where r.Name == AppPermission.Administrador.ToString()
             select ur.UserId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Contexto compartido por los seeders de datos de ejemplo.</summary>
    internal sealed class DatosDemo(MainContext context, Guid usuarioId)
    {
        private readonly DateTime _ahoraUtc = DateTime.UtcNow;

        public MainContext Context { get; } = context;
        public Guid UsuarioId { get; } = usuarioId;

        /// <summary>Hora local, como la que usan las reglas de negocio (fechas de salida, retorno, etc.).</summary>
        public DateTime Ahora { get; } = DateTime.Now;
        public DateOnly Hoy => DateOnly.FromDateTime(Ahora);

        /// <summary>
        /// Activa la entidad y completa su auditoría; el interceptor solo lo hace cuando hay un usuario
        /// autenticado, y al iniciar la aplicación no lo hay.
        /// </summary>
        public T Registrar<T>(T entidad) where T : BaseEntityMetadata, IAuditableEntityMetadata
        {
            entidad.Status = true;
            entidad.CreatedAt = _ahoraUtc;
            entidad.CreatedBy = UsuarioId;
            Context.Add(entidad);
            return entidad;
        }
    }

    /// <summary>
    /// Códigos de departamentos y cargos de ejemplo. Son ficticios (serie 9000): en un ambiente con SIPFFAA
    /// se obtienen de api/externos.
    /// </summary>
    internal static class SipffaaDemo
    {
        public record Item(int Codigo, string Nombre);

        public static class Departamentos
        {
            public static readonly Item Transportacion = new(9001, "Dirección de Transportación (J-4)");
            public static readonly Item Tic = new(9002, "Dirección de Tecnología de la Información y Comunicaciones");
            public static readonly Item RecursosHumanos = new(9003, "Dirección de Personal y Recursos Humanos (J-1)");
            public static readonly Item Despacho = new(9004, "Despacho del Ministro de Defensa");
        }

        public static class Cargos
        {
            public static readonly Item DirectorTransportacion = new(9101, "Director de Transportación");
            public static readonly Item SubdirectorTransportacion = new(9102, "Subdirector de Transportación");
            public static readonly Item EncargadoFlotilla = new(9103, "Encargado de Flotilla Vehicular");
            public static readonly Item DirectorTic = new(9104, "Director de Tecnología");
            public static readonly Item EncargadoNomina = new(9105, "Encargado de Nómina");
            public static readonly Item Ayudante = new(9106, "Ayudante del Ministro");
            public static readonly Item Chofer = new(9107, "Chofer");
        }
    }
}
