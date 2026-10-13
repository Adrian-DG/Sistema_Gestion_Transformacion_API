using Application.Common.DTO;
using Application.Contracts;
using AutoMapper;
using Domain.Common;
using Domain.Common.Errors;
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
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (vehiculo is null)
        {
            return Result.Failure<VehiculoViewModel>(VehiculoErrors.NotFound(request.Id));
        }

        var vehiculoViewModel = mapper.Map<VehiculoViewModel>(vehiculo);

        return Result<VehiculoViewModel>.Success(vehiculoViewModel);
    }
}
