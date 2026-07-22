using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts; // Put IApplicationDbContext here
using Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Vehiculo;

public class VehiculoViewModel
{
    public Guid Id { get; set; }
    public required string Chasis { get; set; }
    public required string Matricula { get; set; }
    public required string Placa { get; set; }
    public required string Marca { get; set; }
    public required string Modelo { get; set; }
    public required int Fabricacion { get; set; }
    public required string Color { get; set; }
}

public class GetVehiculosQuery(IUnitOfWork uow)
    : IRequestHandler<PaginationFilterQuery<VehiculoViewModel>, Result<PagedData<VehiculoViewModel>>>
{
    public async Task<Result<PagedData<VehiculoViewModel>>> Handle(
        PaginationFilterQuery<VehiculoViewModel> request,
        CancellationToken cancellationToken)
    {
        var query = uow.Query<Domain.Entities.Recursos.Vehiculo>();

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            query = query.Where(v => v.Placa.Contains(request.Param) || v.Matricula.Contains(request.Param));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderBy(v => v.Placa)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VehiculoViewModel
            {
                Id = v.Id,
                Chasis = v.Chasis,
                Matricula = v.Matricula,
                Placa = v.Placa,
                Marca = v.Marca != null ? v.Marca.Nombre : string.Empty,
                Modelo = v.Modelo != null ? v.Modelo.Nombre : string.Empty,
                Fabricacion = v.Fabricacion,
                Color = v.Color != null ? v.Color.Nombre : string.Empty
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<VehiculoViewModel>(data, request.PageSize, request.PageNumber, totalRecords));
    }
}