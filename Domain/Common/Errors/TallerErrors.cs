namespace Domain.Common.Errors;

public static class TallerErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Taller.NotFound",
        $"No se encontró la solicitud de taller con id '{id}'.");

    public static Error InvalidStatus(string numero, string estatus) => Error.Conflict(
        "Taller.InvalidStatus",
        $"La solicitud de taller {numero} está {estatus} y no admite esta operación.");

    public static Error SolicitudActiva(string placa, string numero) => Error.Conflict(
        "Taller.SolicitudActiva",
        $"El vehículo {placa} ya tiene la solicitud de taller {numero} abierta.");

    public static Error VehiculoInactivo(string placa) => Error.Conflict(
        "Taller.VehiculoInactivo",
        $"El vehículo {placa} está inactivo (dado de baja); solo se puede reactivar por excepción.");

    public static Error VehiculoEnMision(string placa) => Error.Conflict(
        "Taller.VehiculoEnMision",
        $"El vehículo {placa} está en una misión; registre su retorno antes de ingresarlo al taller.");

    public static Error VehiculoNoInactivo(string placa) => Error.Conflict(
        "Taller.VehiculoNoInactivo",
        $"El vehículo {placa} no está inactivo; solo se reactivan vehículos dados de baja.");

    /// <summary>
    /// No hay existencia para toda la cantidad y no se indicó la compra del faltante. El código le
    /// indica al frontend que debe pedir los datos de la compra.
    /// </summary>
    public static Error CompraRequerida(string pieza, decimal disponible, decimal faltante) => Error.Conflict(
        "Taller.CompraRequerida",
        $"Solo hay {disponible:0.##} de '{pieza}' en inventario y faltan {faltante:0.##}; registre su compra con el costo y el recibo.");

    public static Error CompraInnecesaria(string pieza) => Error.Validation(
        "Taller.CompraInnecesaria",
        $"Hay existencia suficiente de '{pieza}'; se tomará del inventario y no se registra compra.");
}

public static class InventarioPiezasErrors
{
    public static Error PiezaNotFound(Guid id) => Error.NotFound(
        "Pieza.NotFound",
        $"No se encontró la pieza con id '{id}' o está desactivada.");

    public static Error CodigoDuplicado(string codigo) => Error.Conflict(
        "Pieza.CodigoDuplicado",
        $"Ya existe una pieza con el código '{codigo}'.");

    public static Error CodigoBarrasDuplicado(string codigoBarras, string pieza) => Error.Conflict(
        "Pieza.CodigoBarrasDuplicado",
        $"El código de barras '{codigoBarras}' ya está asignado a la pieza '{pieza}'.");

    public static Error ProveedorNotFound(Guid id) => Error.NotFound(
        "Proveedor.NotFound",
        $"No se encontró el proveedor con id '{id}' o está desactivado.");

    public static Error RncDuplicado(string rnc) => Error.Conflict(
        "Proveedor.RncDuplicado",
        $"Ya existe un proveedor con el RNC '{rnc}'.");
}

public static class MecanicoErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Mecanico.NotFound",
        $"No se encontró el mecánico con id '{id}' o está desactivado.");

    public static Error Duplicado(string identificacion) => Error.Conflict(
        "Mecanico.Duplicado",
        $"La persona con identificación '{identificacion}' ya está registrada como mecánico.");
}
