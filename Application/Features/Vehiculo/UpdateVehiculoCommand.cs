using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Vehiculo;

public record UpdateVehiculoCommand(
    Guid VehiculoId, 
    string Placa, 
    Guid ColorId,
    Guid ModeloId, 
    Guid MarcaId) : IRequest<Unit>;