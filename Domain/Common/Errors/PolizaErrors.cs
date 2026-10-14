namespace Domain.Common.Errors;

public static class PolizaErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Poliza.NotFound",
        $"No se encontró la póliza con id '{id}'.");

    public static Error NumeroDuplicado(string numero) => Error.Conflict(
        "Poliza.NumeroDuplicado",
        $"Ya existe una póliza con el número '{numero}'.");

    public static readonly Error AseguradoraInvalida = Error.Validation(
        "Poliza.AseguradoraInvalida",
        "La aseguradora indicada no existe.");
}
