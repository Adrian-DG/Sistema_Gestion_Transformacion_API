using Application.Contracts.Historico;
using Application.Contracts.Recursos;

namespace Application.Contracts
{
    public interface IUnitOfWork
    {
        IVehiculoRepository VehiculoRepository { get; }
        IAsignacionRepository AsignacionRepository { get; }
        IPolizaRepository PolizaRepository { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
