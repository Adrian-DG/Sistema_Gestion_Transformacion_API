namespace Domain.Common.Errors;

public static class PersonaErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Persona.NotFound",
        $"No se encontró la persona con id '{id}'.");

    public static Error Duplicada(string identificacion) => Error.Conflict(
        "Persona.Duplicada",
        $"Ya existe una persona con la identificación '{identificacion}'.");
}
