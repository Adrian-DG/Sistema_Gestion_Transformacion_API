using Application.Common.DTO;
using Application.Common.Response;
using Application.Features.Vehiculo;
using Domain.Common;
using Domain.Entities.Recursos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts.Recursos
{
    public interface IVehiculoRepository
    {
        Task Create(Vehiculo vehiculo, CancellationToken cancellationToken);    
    }
}
