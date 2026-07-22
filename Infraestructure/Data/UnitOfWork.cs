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
        public IAuthenticationRepository AuthenticationRepository => _authenticationRepository 
            ??= new AuthenticationRepository(userManager, signInManager, tokenGenerator);

        public IQueryable<TEntity> Query<TEntity>() where TEntity : class
        {
            return context.Set<TEntity>().AsNoTracking();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return context.SaveChangesAsync(cancellationToken);
        }
    }
}
