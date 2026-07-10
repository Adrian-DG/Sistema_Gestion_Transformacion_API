using Domain.Abstraction;

namespace Application.Contracts.Misc
{
    public interface ICatalogRepository<TEntity> where TEntity : NamedEntityMetadata
    {
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken);
    }
}
