namespace Domain.Common.Errors;

public static class CombustibleErrors
{
    public static Error ExistenciaInsuficiente(string tipoCombustible, decimal disponible, decimal solicitado) => Error.Conflict(
        "Combustible.ExistenciaInsuficiente",
        $"No hay suficiente {tipoCombustible} disponible para despacho. Disponible: {disponible:0.##} gal, solicitado: {solicitado:0.##} gal.");

    public static Error NoEsSuplidorCombustible(string proveedor) => Error.Validation(
        "Combustible.NoEsSuplidorCombustible",
        $"El proveedor '{proveedor}' no está registrado como suplidor de combustible.");

    public static Error AsignacionNoAprobada(Guid asignacionId) => Error.Conflict(
        "Combustible.AsignacionNoAprobada",
        $"Solo se puede despachar combustible a asignaciones aprobadas. Asignación: '{asignacionId}'.");
}
