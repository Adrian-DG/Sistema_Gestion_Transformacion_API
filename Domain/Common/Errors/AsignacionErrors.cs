namespace Domain.Common.Errors;

public static class AsignacionErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Asignacion.NotFound",
        $"No se encontró la asignación con id '{id}'.");
}
