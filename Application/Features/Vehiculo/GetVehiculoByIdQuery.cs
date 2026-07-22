using Application.Common.DTO;
using Application.Contracts;
using AutoMapper;
using Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Vehiculo;

public class GetVehiculoQueryHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<IDEntityFilterQuery<VehiculoViewModel>, Result<VehiculoViewModel>>
{
    public async Task<Result<VehiculoViewModel>> Handle(IDEntityFilterQuery<VehiculoViewModel> request, CancellationToken cancellationToken)
    {
        var vehiculo = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .FirstAsync(v => v.Id == request.Id, cancellationToken);      
    
        var vehiculoViewModel = mapper.Map<VehiculoViewModel>(vehiculo);

        return Result<VehiculoViewModel>.Success(vehiculoViewModel);
    }
}
