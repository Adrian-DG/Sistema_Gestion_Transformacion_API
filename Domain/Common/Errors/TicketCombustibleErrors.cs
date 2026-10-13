namespace Domain.Common.Errors;

public static class TicketCombustibleErrors
{
    public static Error NotFound(string numero) => Error.NotFound(
        "TicketCombustible.NotFound",
        $"No se encontró un ticket vigente con el número '{numero}'.");

    public static Error NoDisponible(string numero, string estatusActual) => Error.Conflict(
        "TicketCombustible.NoDisponible",
        $"El ticket '{numero}' no puede utilizarse porque se encuentra '{estatusActual}'.");
}
