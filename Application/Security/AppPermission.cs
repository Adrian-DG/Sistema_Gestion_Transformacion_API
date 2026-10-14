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
    ReactivarVehiculos
}
