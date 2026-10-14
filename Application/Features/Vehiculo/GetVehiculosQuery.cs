using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Vehiculo;

public class VehiculoViewModel
{
    public Guid Id { get; set; }
    public required string Chasis { get; set; }
    public required string Matricula { get; set; }
    public required string Placa { get; set; }
    public string? Ficha { get; set; }
    public required string Marca { get; set; }
    public required string Modelo { get; set; }
    public required int Fabricacion { get; set; }
    public required string Color { get; set; }
    public required string Tipo { get; set; }

    /// <summary>Disponible, NoDisponible (en misión), EnMantenimiento o Inactivo; ver EstatusVehiculos.</summary>
    public string Estatus { get; set; } = string.Empty;
}

/// <summary>Busca por placa, ficha, matrícula, chasis, marca o modelo.</summary>
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
            var texto = request.Param.Trim();
            query = query.Where(v =>
                v.Placa.Contains(texto) ||
                (v.Ficha != null && v.Ficha.Contains(texto)) ||
                v.Matricula.Contains(texto) ||
                v.Chasis.Contains(texto) ||
                v.Marca!.Nombre.Contains(texto) ||
                v.Modelo!.Nombre.Contains(texto));
        }

        var ahora = DateTime.Now;
        var enMision = EstatusVehiculos.EnMision(ahora);
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
                Ficha = v.Ficha,
                Marca = v.Marca != null ? v.Marca.Nombre : string.Empty,
                Modelo = v.Modelo != null ? v.Modelo.Nombre : string.Empty,
                Fabricacion = v.Fabricacion,
                Color = v.Color != null ? v.Color.Nombre : string.Empty,
                Tipo = v.Tipo != null ? v.Tipo.Nombre : string.Empty,
                Estatus = EstatusVehiculos.Resolver(v.Estado, v.Asignaciones!.AsQueryable().Any(enMision)).ToString()
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<VehiculoViewModel>(data, request.PageSize, request.PageNumber, totalRecords));
    }
}
