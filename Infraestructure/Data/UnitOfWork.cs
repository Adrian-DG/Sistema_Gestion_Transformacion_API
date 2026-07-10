using Application.Contracts;
using Application.Contracts.Historico;
using Application.Contracts.Recursos;
using Infraestructure.Repositories.Historico;
using Infraestructure.Repositories.Recursos;

namespace Infraestructure.Data
{
    public class UnitOfWork(MainContext context) : IUnitOfWork
    {
        private IVehiculoRepository? _vehiculoRepository;
        private IAsignacionRepository? _asignacionRepository;
        private IPolizaRepository? _polizaRepository;

        public IVehiculoRepository VehiculoRepository => _vehiculoRepository ??= new VehiculoRepository(context);
        public IAsignacionRepository AsignacionRepository => _asignacionRepository ??= new AsignacionRepository(context);
        public IPolizaRepository PolizaRepository => _polizaRepository ??= new PolizaRepository(context);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return context.SaveChangesAsync(cancellationToken);
        }
    }
}
