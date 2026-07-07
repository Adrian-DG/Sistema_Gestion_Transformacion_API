using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Vehiculo;

public class VehiculoViewModel
{
    public Guid Id { get; set; }
    public required string Chasis { get; set; }
    public required string Placa { get; set; }
    public required string Marca { get; set; }
    public required string Modelo { get; set; }
    public required int Fabricacion { get; set; }
    public required string Color { get; set; }
}

public class GetVehiculosQuery(IUnitOfWork uow) : IRequestHandler<PaginationFilterQuery<VehiculoViewModel>, PagedData<VehiculoViewModel>>
{
    public async Task<PagedData<VehiculoViewModel>> Handle(PaginationFilterQuery<VehiculoViewModel> request, CancellationToken cancellationToken)
    {
        return await uow.VehiculoRepository.Get(request, cancellationToken);
    }
}

