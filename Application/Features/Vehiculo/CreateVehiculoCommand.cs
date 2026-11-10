using Application.Contracts;
using Domain.Common;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Vehiculo;

/// <summary>
/// Ingreso de un vehículo a la flota, nuevo o usado. Exige el kilometraje con que ingresa (0 si es
/// nuevo), indicar si tiene garantía (y sus datos) y las cuatro fotos: frente, lados y trasera.
/// Queda operativo. Chasis, matrícula, placa y ficha se guardan en mayúsculas.
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
    CondicionVehiculo Condicion,
    int? Kilometraje,
    bool? TieneGarantia,
    List<FotoVehiculoRequest> Fotos,
    string? Ficha = null,
    DateOnly? GarantiaVence = null,
    int? GarantiaKilometraje = null,
    Guid? GarantiaTallerId = null
) : IRequest<Result<Guid>>, IDatosVehiculo;

public class CreateVehiculoCommandValidator : DatosVehiculoValidator<CreateVehiculoCommand>
{
    private static readonly VistaVehiculo[] VistasRequeridas = Enum.GetValues<VistaVehiculo>();

    public CreateVehiculoCommandValidator()
    {
        RuleFor(x => x.Condicion).IsInEnum().WithMessage("Indique si el vehículo es nuevo o usado.");

        // Al ingresar, la garantía debe estar vigente: si ya venció, el vehículo no tiene garantía.
        When(x => x.TieneGarantia == true, () =>
        {
            RuleFor(x => x.GarantiaVence)
                .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.Now))
                .When(x => x.GarantiaVence is not null)
                .WithMessage("La garantía ya venció: indique que el vehículo no tiene garantía.");
            RuleFor(x => x.GarantiaKilometraje)
                .GreaterThan(x => x.Kilometraje)
                .When(x => x.GarantiaKilometraje is not null && x.Kilometraje is not null)
                .WithMessage("El vehículo ya superó el kilometraje de la garantía: indique que no tiene garantía.");
        });

        RuleFor(x => x.Fotos)
            .Must(f => f is not null && f.Count == VistasRequeridas.Length && VistasRequeridas.All(v => f.Count(x => x.Vista == v) == 1))
            .WithMessage("Tome una foto de cada vista del vehículo: frente, lado izquierdo, lado derecho y trasera.");
        RuleForEach(x => x.Fotos).SetValidator(new FotoVehiculoRequestValidator());
    }
}

public class CreateVehiculoCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateVehiculoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVehiculoCommand request, CancellationToken cancellationToken)
    {
        var validacion = await VehiculoReglas.ValidarAsync(uow, request, null, cancellationToken);
        if (validacion.IsFailure) return Result.Failure<Guid>(validacion.Error);

        var vehiculo = new Domain.Entities.Recursos.Vehiculo
        {
            Id = Guid.NewGuid(),
            Chasis = request.Chasis,
            Matricula = request.Matricula,
            Placa = request.Placa,
            Condicion = request.Condicion,
            KilometrajeIngreso = request.Kilometraje!.Value,
            Estado = EstadoVehiculo.Operativo,
            Status = true
        };
        VehiculoReglas.Aplicar(vehiculo, request);

        // Vehículo y fotos se guardan juntos: sin las cuatro fotos no hay ingreso.
        await uow.VehiculoRepository.Create(vehiculo, cancellationToken);
        await uow.AddRangeAsync(request.Fotos.Select(f => f.ToEntity(vehiculo.Id)), cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(vehiculo.Id);
    }
}
