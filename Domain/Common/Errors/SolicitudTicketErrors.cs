namespace Domain.Common.Errors;

public static class SolicitudTicketErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "SolicitudTicket.NotFound",
        $"No se encontró la solicitud de tickets con id '{id}'.");

    public static Error InvalidStatus(Guid id, string estatusActual) => Error.Conflict(
        "SolicitudTicket.InvalidStatus",
        $"La solicitud de tickets con id '{id}' no puede cambiar de estado porque se encuentra '{estatusActual}'.");

    public static Error MotivoRequired => Error.Validation(
        "SolicitudTicket.MotivoRequired",
        "Debe indicar el motivo del rechazo o la cancelación.");

    public static Error TieneTicketsUtilizados(Guid id) => Error.Conflict(
        "SolicitudTicket.TieneTicketsUtilizados",
        $"La solicitud de tickets con id '{id}' no puede cancelarse porque tiene tickets utilizados.");
}
