namespace Application.Security;

public enum AppPolicy
{
    CanManageVehiculos,
    CanManageAsignaciones,
    CanManagePolizas,
    CanManageCombustible,
    CanManagePersonas,
    CanManageUsuarios,
    /// <summary>Personalización del frontend; solo Administrador.</summary>
    CanManageConfiguracion
}
