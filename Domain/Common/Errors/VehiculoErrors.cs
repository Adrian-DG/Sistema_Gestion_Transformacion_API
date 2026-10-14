namespace Domain.Common.Errors;

public static class VehiculoErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Vehiculo.NotFound",
        $"No se encontró el vehículo con id '{id}'.");

    public static Error NotAvailable(Guid id) => Error.Conflict(
        "Vehiculo.NotAvailable",
        $"El vehículo con id '{id}' no está disponible para asignación.");

    /// <param name="campo">Placa, Chasis o Matricula; forma parte del código para ubicar el error en el formulario.</param>
    public static Error Duplicado(string campo, string descripcion, string valor) => Error.Conflict(
        $"Vehiculo.{campo}Duplicado",
        $"Ya existe un vehículo con {descripcion} '{valor}'.");

    public static Error CatalogoInvalido(string mensaje) => Error.Validation(
        "Vehiculo.CatalogoInvalido",
        mensaje);
}
