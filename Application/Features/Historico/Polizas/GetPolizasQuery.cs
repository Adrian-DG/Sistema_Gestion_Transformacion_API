using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using AutoMapper;
using Domain.Common;
using MediatR;

namespace Application.Features.Historico.Polizas;
public class PolizaViewModel
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public required string Vehiculo { get; set; }
    public required string NumeroPoliza { get; set; }
    public required string Aseguradora { get; set; }
    public DateOnly FechaExpedicion { get; set; }
    public DateOnly FechaEfectividad { get; set; }
    public DateOnly FechaVencimiento { get; set; }
}

public class GetPolizasQuery(IUnitOfWork unitOfWork) : IRequestHandler<PaginationFilterQuery<PolizaViewModel>, Result<PagedData<PolizaViewModel>>>
{
    public async Task<Result<PagedData<PolizaViewModel>>> Handle(PaginationFilterQuery<PolizaViewModel> request, CancellationToken cancellationToken)
    {
        var data = await unitOfWork.PolizaRepository.Get(request, cancellationToken);
        return Result.Success(data);
    }
}
