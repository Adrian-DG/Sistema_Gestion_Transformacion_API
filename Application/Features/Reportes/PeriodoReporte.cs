using FluentValidation;
using System.Globalization;

namespace Application.Features.Reportes;

public enum TipoPeriodo
{
    Diario = 1,
    Semanal = 2,
    Mensual = 3,
    Rango = 4
}

public enum AgrupacionSerie
{
    Dia = 1,
    Mes = 2
}

/// <summary>
/// Parámetros de período comunes a los reportes. Diario, Semanal y Mensual toman el día, la semana
/// (lunes a domingo) o el mes que contiene <see cref="Fecha"/> (hoy si se omite); Rango usa Desde/Hasta.
/// </summary>
public interface IPeriodoRequest
{
    TipoPeriodo Periodo { get; }
    DateOnly? Fecha { get; }
    DateOnly? Desde { get; }
    DateOnly? Hasta { get; }
}

public class PeriodoRequestValidator<T> : AbstractValidator<T> where T : IPeriodoRequest
{
    private const int MaxDiasRango = 731;

    public PeriodoRequestValidator()
    {
        RuleFor(x => x.Periodo).IsInEnum();

        When(x => x.Periodo == TipoPeriodo.Rango, () =>
        {
            RuleFor(x => x.Desde).NotNull().WithMessage("Debe indicar la fecha inicial del rango.");
            RuleFor(x => x.Hasta).NotNull().WithMessage("Debe indicar la fecha final del rango.");
            RuleFor(x => x)
                .Must(x => x.Desde <= x.Hasta)
                .When(x => x.Desde.HasValue && x.Hasta.HasValue)
                .WithMessage("La fecha inicial no puede ser posterior a la final.")
                .Must(x => x.Hasta!.Value.DayNumber - x.Desde!.Value.DayNumber < MaxDiasRango)
                .When(x => x.Desde.HasValue && x.Hasta.HasValue)
                .WithMessage($"El rango no puede superar {MaxDiasRango} días.");
        });
    }
}

/// <summary>Período resuelto: [Desde, HastaExclusivo).</summary>
public record PeriodoReporte(TipoPeriodo Tipo, DateTime Desde, DateTime HastaExclusivo, AgrupacionSerie Agrupacion)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    /// <summary>Con rangos de más de 3 meses la serie se agrupa por mes.</summary>
    private const int MaxDiasSerieDiaria = 92;

    public DateOnly FechaInicial => DateOnly.FromDateTime(Desde);
    public DateOnly FechaFinal => DateOnly.FromDateTime(HastaExclusivo.AddDays(-1));

    public string Descripcion => Tipo switch
    {
        TipoPeriodo.Diario => $"Día {FechaInicial.ToString("dd/MM/yyyy", Cultura)}",
        TipoPeriodo.Mensual => $"Mes de {Desde.ToString("MMMM yyyy", Cultura)}",
        _ => $"Del {FechaInicial.ToString("dd/MM/yyyy", Cultura)} al {FechaFinal.ToString("dd/MM/yyyy", Cultura)}"
    };

    public static PeriodoReporte Resolver(IPeriodoRequest request)
    {
        var fecha = request.Fecha ?? DateOnly.FromDateTime(DateTime.Now);

        var (desde, hasta) = request.Periodo switch
        {
            TipoPeriodo.Diario => (fecha, fecha),
            TipoPeriodo.Semanal => SemanaDe(fecha),
            TipoPeriodo.Mensual => (new DateOnly(fecha.Year, fecha.Month, 1),
                                    new DateOnly(fecha.Year, fecha.Month, 1).AddMonths(1).AddDays(-1)),
            _ => (request.Desde!.Value, request.Hasta!.Value)
        };

        var agrupacion = hasta.DayNumber - desde.DayNumber >= MaxDiasSerieDiaria ? AgrupacionSerie.Mes : AgrupacionSerie.Dia;

        return new PeriodoReporte(
            request.Periodo,
            desde.ToDateTime(TimeOnly.MinValue),
            hasta.AddDays(1).ToDateTime(TimeOnly.MinValue),
            agrupacion);
    }

    /// <summary>Inicio del intervalo de la serie al que pertenece la fecha.</summary>
    public DateOnly Intervalo(DateTime fecha) => Agrupacion == AgrupacionSerie.Mes
        ? new DateOnly(fecha.Year, fecha.Month, 1)
        : DateOnly.FromDateTime(fecha);

    /// <summary>Todos los intervalos del período, para que la serie no tenga huecos.</summary>
    public IEnumerable<DateOnly> Intervalos()
    {
        for (var actual = Intervalo(Desde); actual <= FechaFinal;
             actual = Agrupacion == AgrupacionSerie.Mes ? actual.AddMonths(1) : actual.AddDays(1))
        {
            yield return actual;
        }
    }

    public string Sufijo => $"{FechaInicial:yyyyMMdd}-{FechaFinal:yyyyMMdd}";

    private static (DateOnly, DateOnly) SemanaDe(DateOnly fecha)
    {
        var diasDesdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
        var lunes = fecha.AddDays(-diasDesdeLunes);
        return (lunes, lunes.AddDays(6));
    }
}

public class PeriodoViewModel
{
    public required string Tipo { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public required string Agrupacion { get; set; }
    public required string Descripcion { get; set; }

    public static PeriodoViewModel From(PeriodoReporte periodo) => new()
    {
        Tipo = periodo.Tipo.ToString(),
        Desde = periodo.FechaInicial,
        Hasta = periodo.FechaFinal,
        Agrupacion = periodo.Agrupacion.ToString(),
        Descripcion = periodo.Descripcion
    };
}

public class ConteoViewModel
{
    public required string Nombre { get; set; }
    public int Cantidad { get; set; }
    public decimal? Galones { get; set; }
}
