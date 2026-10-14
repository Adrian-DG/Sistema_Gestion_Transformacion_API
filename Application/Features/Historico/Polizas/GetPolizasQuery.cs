using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico.Polizas;

public class PolizaViewModel
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public required string Placa { get; set; }
    public required string Vehiculo { get; set; }
    public required string NumeroPoliza { get; set; }
    public required string Aseguradora { get; set; }
    public DateOnly FechaExpedicion { get; set; }
    public DateOnly FechaEfectividad { get; set; }
    public DateOnly FechaVencimiento { get; set; }

    /// <summary>Vigente, PorVencer o Vencida, a la fecha de la consulta.</summary>
    public string Cobertura { get; set; } = string.Empty;
    public int DiasRestantes { get; set; }
}

/// <summary>Póliza con los Ids de vehículo y aseguradora, para el formulario de edición.</summary>
public class PolizaDetalleViewModel
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public required string Vehiculo { get; set; }
    public Guid AseguradoraId { get; set; }
    public required string Aseguradora { get; set; }
    public required string Numero { get; set; }
    public DateOnly FechaExpedicion { get; set; }
    public DateOnly FechaEfectividad { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public string Cobertura { get; set; } = string.Empty;
    public int DiasRestantes { get; set; }
}

public class GetPolizasQuery(IUnitOfWork unitOfWork) :
    IRequestHandler<PaginationFilterQuery<PolizaViewModel>, Result<PagedData<PolizaViewModel>>>,
    IRequestHandler<IDEntityFilterQuery<PolizaDetalleViewModel>, Result<PolizaDetalleViewModel>>
{
    public async Task<Result<PagedData<PolizaViewModel>>> Handle(PaginationFilterQuery<PolizaViewModel> request, CancellationToken cancellationToken)
    {
        var data = await unitOfWork.PolizaRepository.Get(request, cancellationToken);
        return Result.Success(data);
    }

    public async Task<Result<PolizaDetalleViewModel>> Handle(IDEntityFilterQuery<PolizaDetalleViewModel> request, CancellationToken cancellationToken)
    {
        var poliza = await unitOfWork.Query<Poliza>()
            .Where(p => p.Id == request.Id)
            .Select(p => new PolizaDetalleViewModel
            {
                Id = p.Id,
                VehiculoId = p.VehiculoId,
                Vehiculo = p.Vehiculo!.Placa + " · " + p.Vehiculo.Marca!.Nombre + " " + p.Vehiculo.Modelo!.Nombre,
                AseguradoraId = p.AseguradoraId,
                Aseguradora = p.Aseguradora!.Nombre,
                Numero = p.Numero,
                FechaExpedicion = p.FechaExpedicion,
                FechaEfectividad = p.FechaEfectividad,
                FechaVencimiento = p.FechaVencimiento
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (poliza is null) return Result.Failure<PolizaDetalleViewModel>(PolizaErrors.NotFound(request.Id));

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        poliza.Cobertura = Poliza.Cobertura(poliza.FechaVencimiento, hoy).ToString();
        poliza.DiasRestantes = Poliza.DiasRestantes(poliza.FechaVencimiento, hoy);
        return Result.Success(poliza);
    }
}
