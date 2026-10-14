using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infraestructure.Data
{
    /// <summary>
    /// Crea el contexto para las herramientas de EF (dotnet ef migrations / database update) sin levantar
    /// la API. Toma DefaultConnection de los appsettings del proyecto API (Development sobreescribe al
    /// base) o de la variable de entorno ConnectionStrings__DefaultConnection.
    /// </summary>
    public class MainContextDesignTimeFactory : IDesignTimeDbContextFactory<MainContext>
    {
        public MainContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(CarpetaApi())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

            var options = new DbContextOptionsBuilder<MainContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new MainContext(options);
        }

        /// <summary>
        /// Las herramientas se ejecutan desde la carpeta del proyecto de inicio (Infraestructure o API) o
        /// desde la raíz de la solución.
        /// </summary>
        private static string CarpetaApi()
        {
            var actual = Directory.GetCurrentDirectory();
            string[] candidatas = [Path.Combine(actual, "..", "API"), Path.Combine(actual, "API"), actual];

            return candidatas.FirstOrDefault(c => File.Exists(Path.Combine(c, "appsettings.json"))) ?? actual;
        }
    }
}
