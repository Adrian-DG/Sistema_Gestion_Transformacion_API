using Application.Contracts.Misc;
using Domain.Abstraction;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infraestructure.Repositories.Misc
{
    public class CatalogRepository<TEntity>(MainContext context, IMemoryCache cache) : ICatalogRepository<TEntity>
        where TEntity : NamedEntityMetadata
    {
        private static readonly string CacheKey = $"catalogo:{typeof(TEntity).Name}";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            if (cache.TryGetValue(CacheKey, out IReadOnlyList<TEntity>? cached) && cached is not null)
            {
                return cached;
            }

            var data = await context.Set<TEntity>()
                .Where(x => x.Status)
                .OrderBy(x => x.Nombre)
                .ToListAsync(cancellationToken);

            cache.Set(CacheKey, (IReadOnlyList<TEntity>)data, CacheDuration);

            return data;
        }
    }
}
