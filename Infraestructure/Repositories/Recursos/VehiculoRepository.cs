using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Recursos;
using Application.Features.Vehiculo;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Recursos;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories.Recursos
{
    public class VehiculoRepository(MainContext context) : IVehiculoRepository
    {
        public async Task Create(Vehiculo vehiculo, CancellationToken cancellationToken)
        {
            await context.Vehiculos.AddAsync(vehiculo, cancellationToken);
        }
    }
}
