using Application.Contracts.Authentication;
using Application.Contracts.Historico;
using Application.Contracts.Recursos;

namespace Application.Contracts
{
    public interface IUnitOfWork
    {
        IVehiculoRepository VehiculoRepository { get; }
        IAsignacionRepository AsignacionRepository { get; }
        IPolizaRepository PolizaRepository { get; }
        IAuthenticationRepository AuthenticationRepository { get; }

        /// <summary>Consulta de solo lectura (sin seguimiento de cambios).</summary>
        IQueryable<TEntity> Query<TEntity>() where TEntity : class;

        /// <summary>Consulta con seguimiento de cambios, para entidades que se van a modificar.</summary>
        IQueryable<TEntity> Tracked<TEntity>() where TEntity : class;

        Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class;
        Task AddRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken) where TEntity : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
