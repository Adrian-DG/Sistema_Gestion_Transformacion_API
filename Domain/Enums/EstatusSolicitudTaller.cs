namespace Domain.Enums
{
    /// <summary>
    /// Pendiente (registrada, el vehículo aún no entra) → EnTaller (ingresó, con mecánico asignado) →
    /// Finalizada. Una pendiente se puede rechazar; una pendiente o en taller, cancelar.
    /// </summary>
    public enum EstatusSolicitudTaller
    {
        Pendiente = 0,
        EnTaller = 1,
        Finalizada = 2,
        Cancelada = 3,
        Rechazada = 4
    }
}
