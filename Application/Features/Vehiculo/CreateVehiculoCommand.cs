using Application.Contracts;
using AutoMapper;
using Domain.Common;
using MediatR;

namespace Application.Features.Vehiculo;

/// <summary>
/// Registra un vehículo disponible con su kilometraje y, si es nuevo, su garantía. Chasis, matrícula,
/// placa y ficha se guardan en mayúsculas.
/// </summary>
public record CreateVehiculoCommand(
    string Chasis,
    string Matricula,
    string Placa,
    int Fabricacion,
    Guid MarcaId,
    Guid ModeloId,
    Guid TipoId,
    Guid ColorId,
    string? Ficha = null,
    int Kilometraje = 0,
    DateOnly? GarantiaVence = null,
    int? GarantiaKilometraje = null,
    Guid? GarantiaTallerId = null
): IRequest<Result<Guid>>, IDatosVehiculo;

public class CreateVehiculoCommandValidator : DatosVehiculoValidator<CreateVehiculoCommand>;

public class CreateVehiculoCommandHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<CreateVehiculoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVehiculoCommand request, CancellationToken cancellationToken)
    {
        var validacion = await VehiculoReglas.ValidarAsync(uow, request, null, cancellationToken);
        if (validacion.IsFailure) return Result.Failure<Guid>(validacion.Error);

        var vehiculo = mapper.Map<Domain.Entities.Recursos.Vehiculo>(request);
        VehiculoReglas.Aplicar(vehiculo, request);
        vehiculo.Estado = Domain.Enums.EstadoVehiculo.Operativo;
        vehiculo.Status = true;

        await uow.VehiculoRepository.Create(vehiculo, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(vehiculo.Id);
    }
}
