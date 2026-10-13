namespace Domain.Enums
{
    /// <summary>
    /// Ciclo de vida de una solicitud (de vehículo o de tickets de combustible).
    /// Pendiente → Aprobada → Finalizada, o bien Rechazada / Cancelada.
    /// </summary>
    public enum EstatusSolicitud
    {
        Pendiente = 0,
        Aprobada = 1,
        Cancelada = 2,
        Rechazada = 3,
        Finalizada = 4
    }
}
