namespace Domain.Common.Errors;

public static class VehiculoErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Vehiculo.NotFound",
        $"No se encontró el vehículo con id '{id}'.");

    public static Error NotAvailable(Guid id) => Error.Conflict(
        "Vehiculo.NotAvailable",
        $"El vehículo con id '{id}' no está disponible para asignación.");
}
