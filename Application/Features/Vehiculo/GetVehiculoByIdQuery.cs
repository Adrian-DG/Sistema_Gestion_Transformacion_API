using Application.Common.DTO;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Vehiculo;

/// <summary>Vehículo con los Ids de sus catálogos, para el formulario de edición.</summary>
public class VehiculoDetalleViewModel
{
    public Guid Id { get; set; }
    public required string Chasis { get; set; }
    public required string Matricula { get; set; }
    public required string Placa { get; set; }
    public string? Ficha { get; set; }
    public int Fabricacion { get; set; }
    public Guid MarcaId { get; set; }
    public required string Marca { get; set; }
    public Guid ModeloId { get; set; }
    public required string Modelo { get; set; }
    public Guid TipoId { get; set; }
    public required string Tipo { get; set; }
    public Guid ColorId { get; set; }
    public required string Color { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public CondicionVehiculo Condicion { get; set; }
    /// <summary>Kilometraje con que ingresó a la flota.</summary>
    public int KilometrajeIngreso { get; set; }
    /// <summary>Fecha de ingreso (registro) a la flota.</summary>
    public DateTime FechaIngreso { get; set; }
    public int Kilometraje { get; set; }
    public DateOnly? GarantiaVence { get; set; }
    public int? GarantiaKilometraje { get; set; }
    public Guid? GarantiaTallerId { get; set; }
    public string? GarantiaTaller { get; set; }
    /// <summary>Garantía vigente hoy (fecha y kilometraje).</summary>
    public bool EnGarantia { get; set; }
    /// <summary>Fotos vigentes (una por vista); el archivo se descarga por su vista.</summary>
    public List<FotoVehiculoViewModel> Fotos { get; set; } = [];
}

public class GetVehiculoQueryHandler(IUnitOfWork uow) : IRequestHandler<IDEntityFilterQuery<VehiculoDetalleViewModel>, Result<VehiculoDetalleViewModel>>
{
    public async Task<Result<VehiculoDetalleViewModel>> Handle(IDEntityFilterQuery<VehiculoDetalleViewModel> request, CancellationToken cancellationToken)
    {
        var enMision = EstatusVehiculos.EnMision(DateTime.Now);
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var vehiculo = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .Where(v => v.Id == request.Id)
            .Select(v => new VehiculoDetalleViewModel
            {
                Id = v.Id,
                Chasis = v.Chasis,
                Matricula = v.Matricula,
                Placa = v.Placa,
                Ficha = v.Ficha,
                Fabricacion = v.Fabricacion,
                MarcaId = v.MarcaId,
                Marca = v.Marca!.Nombre,
                ModeloId = v.ModeloId,
                Modelo = v.Modelo!.Nombre,
                TipoId = v.TipoId,
                Tipo = v.Tipo!.Nombre,
                ColorId = v.ColorId,
                Color = v.Color!.Nombre,
                Estatus = EstatusVehiculos.Resolver(v.Estado, v.Asignaciones!.AsQueryable().Any(enMision)).ToString(),
                Condicion = v.Condicion,
                KilometrajeIngreso = v.KilometrajeIngreso,
                FechaIngreso = v.CreatedAt,
                Kilometraje = v.Kilometraje,
                Fotos = v.Fotos
                    .Where(f => f.Status)
                    .OrderBy(f => f.Vista)
                    .Select(f => new FotoVehiculoViewModel { Id = f.Id, Vista = f.Vista, NombreArchivo = f.NombreArchivo, Fecha = f.CreatedAt })
                    .ToList(),
                GarantiaVence = v.GarantiaVence,
                GarantiaKilometraje = v.GarantiaKilometraje,
                GarantiaTallerId = v.GarantiaTallerId,
                GarantiaTaller = v.GarantiaTaller != null ? v.GarantiaTaller.Nombre : null,
                EnGarantia = v.GarantiaVence >= hoy && (v.GarantiaKilometraje == null || v.Kilometraje <= v.GarantiaKilometraje)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return vehiculo is null
            ? Result.Failure<VehiculoDetalleViewModel>(VehiculoErrors.NotFound(request.Id))
            : Result.Success(vehiculo);
    }
}
