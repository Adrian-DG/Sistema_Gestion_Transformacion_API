namespace Application.Security;

public enum AppPermission
{
    Administrador,
    GestionVehiculos,
    GestionAsignaciones,
    GestionPolizas,
    GestionCombustible,
    GestionPersonas,
    GestionUsuarios,
    /// <summary>Solicitudes de taller (ingreso, piezas, cierre) y mecánicos.</summary>
    GestionTaller,
    /// <summary>Piezas, proveedores y entradas al inventario del taller.</summary>
    GestionInventarioPiezas,
    /// <summary>Reactivación excepcional de vehículos dados de baja.</summary>
    ReactivarVehiculos,
    /// <summary>Aprobar o rechazar asignaciones de vehículos (separado de quien las registra).</summary>
    AprobarAsignaciones,
    /// <summary>Aprobar o rechazar solicitudes de tickets de combustible.</summary>
    AprobarTickets,
    /// <summary>Operador de la bomba: canjear tickets y registrar despachos de combustible.</summary>
    DespachoCombustible,
    /// <summary>Solo lectura: listados y reportes de vehículos, pólizas, asignaciones y combustible.</summary>
    Consulta
}
