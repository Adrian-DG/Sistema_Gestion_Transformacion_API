namespace Domain.Enums
{
    /// <summary>
    /// Estatus visible del vehículo. No se guarda: se obtiene de su <see cref="EstadoVehiculo"/> y,
    /// si está operativo, de si tiene una misión (asignación aprobada) en curso.
    /// </summary>
    public enum EstatusVehiculo
    {
        Disponible = 0,
        NoDisponible = 1,
        EnMantenimiento = 2,
        Inactivo = 3
    }
}
