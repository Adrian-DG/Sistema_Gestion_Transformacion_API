namespace Domain.Enums
{
    /// <summary>Motivo por el que el vehículo entra al taller.</summary>
    public enum TipoSolicitudTaller
    {
        Mantenimiento = 1,
        Reparacion = 2,
        /// <summary>Revisión para dictaminar si el vehículo se da de baja.</summary>
        EvaluacionBaja = 3
    }
}
