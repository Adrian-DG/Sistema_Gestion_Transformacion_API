using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Misc;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Vehiculo;

/// <summary>Datos de un vehículo comunes al registro y la edición.</summary>
public interface IDatosVehiculo
{
    string Chasis { get; }
    string Matricula { get; }
    string Placa { get; }
    int Fabricacion { get; }
    Guid MarcaId { get; }
    Guid ModeloId { get; }
    Guid TipoId { get; }
    Guid ColorId { get; }
}

public class DatosVehiculoValidator<T> : AbstractValidator<T> where T : IDatosVehiculo
{
    public DatosVehiculoValidator()
    {
        RuleFor(x => x.Chasis)
            .NotEmpty().WithMessage("El chasis es obligatorio.")
            .MaximumLength(50).WithMessage("El chasis debe tener un máximo de 50 caracteres.");

        RuleFor(x => x.Matricula)
            .NotEmpty().WithMessage("La matrícula es obligatoria.")
            .MaximumLength(30).WithMessage("La matrícula debe tener un máximo de 30 caracteres.");

        RuleFor(x => x.Placa)
            .NotEmpty().WithMessage("La placa es obligatoria.")
            .MaximumLength(10).WithMessage("La placa debe tener un máximo de 10 caracteres.");

        RuleFor(x => x.Fabricacion)
            .InclusiveBetween(1900, DateTime.Now.Year + 1)
            .WithMessage($"El año de fabricación debe estar entre 1900 y {DateTime.Now.Year + 1}.");

        RuleFor(x => x.MarcaId).NotEmpty().WithMessage("La marca es obligatoria.");
        RuleFor(x => x.ModeloId).NotEmpty().WithMessage("El modelo es obligatorio.");
        RuleFor(x => x.TipoId).NotEmpty().WithMessage("El tipo de vehículo es obligatorio.");
        RuleFor(x => x.ColorId).NotEmpty().WithMessage("El color es obligatorio.");
    }
}

internal static class VehiculoReglas
{
    /// <summary>Normaliza los identificadores del vehículo: sin espacios a los lados y en mayúsculas.</summary>
    public static string Normalizar(string valor) => valor.Trim().ToUpperInvariant();

    /// <summary>
    /// Verifica que los catálogos existan y estén activos, que el modelo sea de la marca indicada y que
    /// chasis, matrícula y placa no estén registrados en otro vehículo.
    /// </summary>
    public static async Task<Result> ValidarAsync(IUnitOfWork uow, IDatosVehiculo datos, Guid? excluirId, CancellationToken cancellationToken)
    {
        var modelo = await uow.Query<Modelo>()
            .Where(m => m.Id == datos.ModeloId && m.Status)
            .Select(m => new { m.MarcaId })
            .FirstOrDefaultAsync(cancellationToken);

        if (modelo is null || !await uow.Query<Marca>().AnyAsync(m => m.Id == datos.MarcaId && m.Status, cancellationToken))
        {
            return Result.Failure(VehiculoErrors.CatalogoInvalido("La marca o el modelo indicados no existen."));
        }

        if (modelo.MarcaId != datos.MarcaId)
        {
            return Result.Failure(VehiculoErrors.CatalogoInvalido("El modelo indicado no pertenece a la marca seleccionada."));
        }

        if (!await uow.Query<TipoVehiculo>().AnyAsync(t => t.Id == datos.TipoId && t.Status, cancellationToken))
        {
            return Result.Failure(VehiculoErrors.CatalogoInvalido("El tipo de vehículo indicado no existe."));
        }

        if (!await uow.Query<Color>().AnyAsync(c => c.Id == datos.ColorId && c.Status, cancellationToken))
        {
            return Result.Failure(VehiculoErrors.CatalogoInvalido("El color indicado no existe."));
        }

        var chasis = Normalizar(datos.Chasis);
        var matricula = Normalizar(datos.Matricula);
        var placa = Normalizar(datos.Placa);

        var duplicado = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .Where(v => v.Id != excluirId && (v.Chasis == chasis || v.Matricula == matricula || v.Placa == placa))
            .Select(v => new { v.Chasis, v.Matricula, v.Placa })
            .FirstOrDefaultAsync(cancellationToken);

        if (duplicado is not null)
        {
            return Result.Failure(
                duplicado.Placa == placa ? VehiculoErrors.Duplicado("Placa", "la placa", placa) :
                duplicado.Chasis == chasis ? VehiculoErrors.Duplicado("Chasis", "el chasis", chasis) :
                VehiculoErrors.Duplicado("Matricula", "la matrícula", matricula));
        }

        return Result.Success();
    }

    public static void Aplicar(Domain.Entities.Recursos.Vehiculo vehiculo, IDatosVehiculo datos)
    {
        vehiculo.Chasis = Normalizar(datos.Chasis);
        vehiculo.Matricula = Normalizar(datos.Matricula);
        vehiculo.Placa = Normalizar(datos.Placa);
        vehiculo.Fabricacion = datos.Fabricacion;
        vehiculo.MarcaId = datos.MarcaId;
        vehiculo.ModeloId = datos.ModeloId;
        vehiculo.TipoId = datos.TipoId;
        vehiculo.ColorId = datos.ColorId;
    }
}
