using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Entities.Misc;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico.Polizas;

/// <summary>Datos de una póliza comunes al registro y la edición.</summary>
public interface IDatosPoliza
{
    Guid VehiculoId { get; }
    Guid AseguradoraId { get; }
    string Numero { get; }
    DateOnly FechaExpedicion { get; }
    DateOnly FechaEfectividad { get; }
    DateOnly FechaVencimiento { get; }
}

public class DatosPolizaValidator<T> : AbstractValidator<T> where T : IDatosPoliza
{
    public DatosPolizaValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty().WithMessage("El vehículo es obligatorio.");
        RuleFor(x => x.AseguradoraId).NotEmpty().WithMessage("La aseguradora es obligatoria.");
        RuleFor(x => x.Numero).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FechaVencimiento)
            .GreaterThan(x => x.FechaEfectividad)
            .WithMessage("La fecha de vencimiento debe ser mayor a la fecha de efectividad.");
        RuleFor(x => x.FechaExpedicion)
            .LessThan(x => x.FechaVencimiento)
            .WithMessage("La fecha de expedición debe ser anterior a la fecha de vencimiento.");
    }
}

internal static class PolizaReglas
{
    /// <summary>Verifica el vehículo, la aseguradora y que el número no esté en otra póliza.</summary>
    public static async Task<Result> ValidarAsync(IUnitOfWork uow, IDatosPoliza datos, Guid? excluirId, CancellationToken cancellationToken)
    {
        var vehiculo = await uow.VehiculoRepository.GetById(datos.VehiculoId, cancellationToken);
        if (vehiculo.IsFailure) return vehiculo;

        if (!await uow.Query<Aseguradora>().AnyAsync(a => a.Id == datos.AseguradoraId && a.Status, cancellationToken))
        {
            return Result.Failure(PolizaErrors.AseguradoraInvalida);
        }

        var numero = datos.Numero.Trim();
        if (await uow.Query<Poliza>().AnyAsync(p => p.Numero == numero && p.Id != excluirId, cancellationToken))
        {
            return Result.Failure(PolizaErrors.NumeroDuplicado(numero));
        }

        return Result.Success();
    }

    public static void Aplicar(Poliza poliza, IDatosPoliza datos)
    {
        poliza.VehiculoId = datos.VehiculoId;
        poliza.AseguradoraId = datos.AseguradoraId;
        poliza.Numero = datos.Numero.Trim();
        poliza.FechaExpedicion = datos.FechaExpedicion;
        poliza.FechaEfectividad = datos.FechaEfectividad;
        poliza.FechaVencimiento = datos.FechaVencimiento;
    }
}
