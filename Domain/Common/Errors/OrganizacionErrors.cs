namespace Domain.Common.Errors;

public static class OrganizacionErrors
{
    public static Error DependenciaNotFound(Guid id) => Error.NotFound(
        "Dependencia.NotFound",
        $"No se encontró la dependencia con id '{id}'.");

    public static Error CargoNotFound(Guid id) => Error.NotFound(
        "Cargo.NotFound",
        $"No se encontró el cargo con id '{id}'.");

    public static Error PersonaNotFound(Guid id) => Error.NotFound(
        "Persona.NotFound",
        $"No se encontró la persona con id '{id}'.");

    public static Error PersonaDuplicada(string identificacion) => Error.Conflict(
        "Persona.Duplicada",
        $"Ya existe una persona con la identificación '{identificacion}'.");
}
