using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infraestructure.Data.Seed
{
    internal static class SeedDatabaseExtensions
    {
        /// <summary>
        /// Los seeders que se ejecutan al iniciar dependen de las tablas y catálogos que crean las
        /// migraciones; sin base o con migraciones pendientes no deben ejecutarse.
        /// </summary>
        public static async Task<bool> EstaListaParaSeedAsync(this DatabaseFacade database, CancellationToken cancellationToken) =>
            await database.CanConnectAsync(cancellationToken)
            && !(await database.GetPendingMigrationsAsync(cancellationToken)).Any();
    }
}
