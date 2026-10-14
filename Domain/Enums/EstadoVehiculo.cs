namespace Domain.Enums
{
    /// <summary>
    /// Condición del vehículo que registra el taller. Por defecto Operativo; pasa a EnMantenimiento
    /// al ingresar al taller y a Inactivo cuando el mecánico dictamina su baja.
    /// </summary>
    public enum EstadoVehiculo
    {
        Operativo = 0,
        EnMantenimiento = 1,
        Inactivo = 2
    }
}
