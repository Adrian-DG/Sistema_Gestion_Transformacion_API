namespace Domain.Common.Errors;

public static class VehiculoErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Vehiculo.NotFound",
        $"No se encontró el vehículo con id '{id}'.");

    public static Error NotAvailable(Guid id) => Error.Conflict(
        "Vehiculo.NotAvailable",
        $"El vehículo con id '{id}' no está disponible para asignación.");

    /// <summary>El vehículo está en el taller o dado de baja; no se puede asignar.</summary>
    public static Error NoOperativo(Guid id, Enums.EstadoVehiculo estado) => Error.Conflict(
        "Vehiculo.NoOperativo",
        estado == Enums.EstadoVehiculo.Inactivo
            ? $"El vehículo con id '{id}' está inactivo (dado de baja) y no se puede asignar."
            : $"El vehículo con id '{id}' está en mantenimiento y no se puede asignar hasta que salga del taller.");

    /// <param name="campo">Placa, Chasis o Matricula; forma parte del código para ubicar el error en el formulario.</param>
    public static Error Duplicado(string campo, string descripcion, string valor) => Error.Conflict(
        $"Vehiculo.{campo}Duplicado",
        $"Ya existe un vehículo con {descripcion} '{valor}'.");

    /// <summary>El kilometraje nunca baja: el taller y los mantenimientos dependen de él.</summary>
    public static Error KilometrajeMenor(int actual) => Error.Validation(
        "Vehiculo.KilometrajeMenor",
        $"El kilometraje no puede ser menor que el último registrado ({actual:N0} km).");

    public static Error FotoNotFound(Guid vehiculoId, Enums.VistaVehiculo vista) => Error.NotFound(
        "Vehiculo.FotoNotFound",
        $"El vehículo con id '{vehiculoId}' no tiene foto de la vista '{vista}'.");

    public static Error CatalogoInvalido(string mensaje) => Error.Validation(
        "Vehiculo.CatalogoInvalido",
        mensaje);
}
