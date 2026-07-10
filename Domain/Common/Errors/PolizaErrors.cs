namespace Domain.Common.Errors;

public static class PolizaErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Poliza.NotFound",
        $"No se encontró la póliza con id '{id}'.");
}
