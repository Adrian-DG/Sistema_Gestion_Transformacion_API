namespace Application.Security;

public enum AppPolicy
{
    CanManageVehiculos,
    CanManageAsignaciones,
    CanManagePolizas,
    CanManageCombustible,
    CanManagePersonas,
    CanManageUsuarios,
    CanManageTaller,
    CanManageInventarioPiezas,
    CanReactivarVehiculos,
    /// <summary>Personalización del frontend; solo Administrador.</summary>
    CanManageConfiguracion,

    // Lectura (incluye el permiso Consulta y a quienes trabajan en el flujo)
    CanVerVehiculos,
    CanVerPolizas,
    CanVerAsignaciones,
    CanVerCombustible,

    // Aprobaciones y despacho, separados de quien registra
    CanAprobarAsignaciones,
    CanAprobarTickets,
    CanDespacharCombustible,

    /// <summary>Buscar personas registradas y miembros de SIPFFAA (datos personales).</summary>
    CanBuscarPersonal
}
