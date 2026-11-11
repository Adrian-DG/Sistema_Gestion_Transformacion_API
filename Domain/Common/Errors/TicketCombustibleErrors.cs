namespace Domain.Common.Errors;

public static class TicketCombustibleErrors
{
    public static Error NotFound(string numero) => Error.NotFound(
        "TicketCombustible.NotFound",
        $"No se encontró un ticket vigente con el número '{numero}'.");

    public static Error NoDisponible(string numero, string estatusActual) => Error.Conflict(
        "TicketCombustible.NoDisponible",
        $"El ticket '{numero}' no puede utilizarse porque se encuentra '{estatusActual}'.");

    public static Error YaUtilizado(string numero, DateTime fechaUso) => Error.Conflict(
        "TicketCombustible.YaUtilizado",
        $"El ticket '{numero}' ya fue utilizado el {fechaUso:dd/MM/yyyy} a las {fechaUso:h:mm tt}.");

    public static Error CodigoRequerido(string numero) => Error.Validation(
        "TicketCombustible.CodigoRequerido",
        $"Escanee el ticket '{numero}' o escriba también el código de verificación impreso debajo del código de barras.");

    public static Error CodigoInvalido(string numero) => Error.Validation(
        "TicketCombustible.CodigoInvalido",
        $"El código de verificación no corresponde al ticket '{numero}'. Verifique que el ticket sea auténtico.");
}
