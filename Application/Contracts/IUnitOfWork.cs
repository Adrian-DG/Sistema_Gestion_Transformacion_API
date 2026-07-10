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
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
