using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Contracts.Historico;
using Application.Contracts.Recursos;
using Infraestructure.Identity.Authentication;
using Infraestructure.Identity.Models;
using Infraestructure.Repositories.Authentication;
using Infraestructure.Repositories.Historico;
using Infraestructure.Repositories.Recursos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data
{
    public class UnitOfWork(MainContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IJwtTokenGenerator tokenGenerator) : IUnitOfWork
    {
        private IVehiculoRepository? _vehiculoRepository;
        private IAsignacionRepository? _asignacionRepository;
        private IPolizaRepository? _polizaRepository;
        private IAuthenticationRepository? _authenticationRepository;

        public IVehiculoRepository VehiculoRepository => _vehiculoRepository ??= new VehiculoRepository(context);
        public IAsignacionRepository AsignacionRepository => _asignacionRepository ??= new AsignacionRepository(context);
        public IPolizaRepository PolizaRepository => _polizaRepository ??= new PolizaRepository(context);
        public IAuthenticationRepository AuthenticationRepository => _authenticationRepository 
            ??= new AuthenticationRepository(userManager, signInManager, tokenGenerator);

        public IQueryable<TEntity> Query<TEntity>() where TEntity : class
        {
            return context.Set<TEntity>().AsNoTracking();
        }

        public IQueryable<TEntity> Tracked<TEntity>() where TEntity : class
        {
            return context.Set<TEntity>().AsTracking();
        }

        public async Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class
        {
            await context.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        public Task AddRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken) where TEntity : class
        {
            return context.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
        }

        public void Remove<TEntity>(TEntity entity) where TEntity : class
        {
            context.Set<TEntity>().Remove(entity);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return context.SaveChangesAsync(cancellationToken);
        }

        public async Task<TResult> EnTransaccionAsync<TResult>(Func<Task<TResult>> operacion, CancellationToken cancellationToken)
            where TResult : Domain.Common.Result
        {
            await using var transaccion = await context.Database.BeginTransactionAsync(cancellationToken);

            var resultado = await operacion();
            if (resultado.IsSuccess)
            {
                await transaccion.CommitAsync(cancellationToken);
            }
            else
            {
                context.ChangeTracker.Clear();
            }

            return resultado;
        }
    }
}
