namespace Domain.Common.Errors;

public static class AsignacionErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Asignacion.NotFound",
        $"No se encontró la asignación con id '{id}'.");

    public static Error InvalidStatus(Guid id, string estatusActual) => Error.Conflict(
        "Asignacion.InvalidStatus",
        $"La asignación con id '{id}' no puede cambiar de estado porque se encuentra '{estatusActual}'.");

    public static Error MotivoRequired => Error.Validation(
        "Asignacion.MotivoRequired",
        "Debe indicar el motivo del rechazo o la cancelación.");

    public static Error FechaRetornoInvalida => Error.Validation(
        "Asignacion.FechaRetornoInvalida",
        "La fecha de retorno no puede ser anterior a la fecha de salida.");

    public static Error VehiculoOcupado(Guid vehiculoId) => Error.Conflict(
        "Asignacion.VehiculoOcupado",
        $"El vehículo con id '{vehiculoId}' tiene una asignación aprobada que coincide con el período solicitado " +
        $"(incluyendo las {Entities.Historico.Asignacion.HorasReservaPrevia} horas previas a su salida).");
}
