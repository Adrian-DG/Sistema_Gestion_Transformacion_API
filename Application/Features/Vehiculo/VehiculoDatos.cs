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
    string? Ficha { get; }
    int Kilometraje { get; }
    DateOnly? GarantiaVence { get; }
    int? GarantiaKilometraje { get; }
    /// <summary>Taller autorizado (proveedor marcado como tal) donde se hacen los mantenimientos en garantía.</summary>
    Guid? GarantiaTallerId { get; }
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

        RuleFor(x => x.Kilometraje).GreaterThanOrEqualTo(0).WithMessage("El kilometraje no puede ser negativo.");
        RuleFor(x => x.GarantiaKilometraje).GreaterThan(0).WithMessage("El kilometraje límite de la garantía debe ser mayor que 0.");
        RuleFor(x => x.GarantiaVence).NotNull()
            .When(x => x.GarantiaKilometraje is not null || x.GarantiaTallerId is not null)
            .WithMessage("Indique hasta cuándo es válida la garantía.");

        RuleFor(x => x.Ficha)
            .MaximumLength(20).WithMessage("La ficha debe tener un máximo de 20 caracteres.");

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

    /// <summary>La ficha es opcional: vacía se guarda como nula.</summary>
    public static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : Normalizar(valor);

    /// <summary>
    /// Verifica que los catálogos existan y estén activos, que el modelo sea de la marca indicada y que
    /// chasis, matrícula, placa y ficha no estén registrados en otro vehículo.
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

        if (datos.GarantiaTallerId is { } tallerId &&
            !await uow.Query<Domain.Entities.Taller.Proveedor>().AnyAsync(p => p.Id == tallerId && p.Status && p.EsTallerAutorizado, cancellationToken))
        {
            return Result.Failure(VehiculoErrors.CatalogoInvalido("El taller de la garantía no existe o no está marcado como taller autorizado."));
        }

        var chasis = Normalizar(datos.Chasis);
        var matricula = Normalizar(datos.Matricula);
        var placa = Normalizar(datos.Placa);
        var ficha = NormalizarOpcional(datos.Ficha);

        var duplicado = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .Where(v => v.Id != excluirId &&
                (v.Chasis == chasis || v.Matricula == matricula || v.Placa == placa || (ficha != null && v.Ficha == ficha)))
            .Select(v => new { v.Chasis, v.Matricula, v.Placa, v.Ficha })
            .FirstOrDefaultAsync(cancellationToken);

        if (duplicado is not null)
        {
            return Result.Failure(
                duplicado.Placa == placa ? VehiculoErrors.Duplicado("Placa", "la placa", placa) :
                duplicado.Chasis == chasis ? VehiculoErrors.Duplicado("Chasis", "el chasis", chasis) :
                duplicado.Matricula == matricula ? VehiculoErrors.Duplicado("Matricula", "la matrícula", matricula) :
                VehiculoErrors.Duplicado("Ficha", "la ficha", ficha!));
        }

        return Result.Success();
    }

    public static void Aplicar(Domain.Entities.Recursos.Vehiculo vehiculo, IDatosVehiculo datos)
    {
        vehiculo.Chasis = Normalizar(datos.Chasis);
        vehiculo.Matricula = Normalizar(datos.Matricula);
        vehiculo.Placa = Normalizar(datos.Placa);
        vehiculo.Ficha = NormalizarOpcional(datos.Ficha);
        vehiculo.Kilometraje = datos.Kilometraje;
        vehiculo.GarantiaVence = datos.GarantiaVence;
        vehiculo.GarantiaKilometraje = datos.GarantiaVence is null ? null : datos.GarantiaKilometraje;
        vehiculo.GarantiaTallerId = datos.GarantiaVence is null ? null : datos.GarantiaTallerId;
        vehiculo.Fabricacion = datos.Fabricacion;
        vehiculo.MarcaId = datos.MarcaId;
        vehiculo.ModeloId = datos.ModeloId;
        vehiculo.TipoId = datos.TipoId;
        vehiculo.ColorId = datos.ColorId;
    }
}
