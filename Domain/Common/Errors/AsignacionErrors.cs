namespace Domain.Common.Errors;

public static class AsignacionErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Asignacion.NotFound",
        $"No se encontró la asignación con id '{id}'.");

    public static Error PersonaNotFound(Guid personaId) => Error.NotFound(
        "Asignacion.PersonaNotFound",
        $"No se encontró la persona con id '{personaId}'.");

    public static Error InvalidStatus(Guid id, string estatusActual) => Error.Conflict(
        "Asignacion.InvalidStatus",
        $"La asignación con id '{id}' no puede cambiar de estado porque se encuentra '{estatusActual}'.");

    public static Error MotivoCancelacionRequired => Error.Validation(
        "Asignacion.MotivoCancelacionRequired",
        "Debe indicar el motivo de cancelación.");
}
