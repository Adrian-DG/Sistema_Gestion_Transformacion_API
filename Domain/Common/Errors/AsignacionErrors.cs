using System.Globalization;

namespace Domain.Common.Errors;

/// <summary>
/// Errores de las asignaciones. Los mensajes se muestran tal cual al usuario: indican el vehículo por su
/// placa, las personas por su nombre y qué hacer para resolverlo.
/// </summary>
public static class AsignacionErrors
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    private static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy h:mm tt", Cultura);

    public static Error NotFound(Guid id) => Error.NotFound(
        "Asignacion.NotFound",
        "No se encontró la asignación. Puede que haya sido eliminada o que el enlace sea incorrecto.");

    public static Error InvalidStatus(Guid id, string estatusActual) => Error.Conflict(
        "Asignacion.InvalidStatus",
        $"La asignación no puede cambiar de estado porque se encuentra '{estatusActual}'. Actualice el listado.");

    public static Error MotivoRequired => Error.Validation(
        "Asignacion.MotivoRequired",
        "Debe indicar el motivo del rechazo o la cancelación.");

    public static Error FechaRetornoInvalida => Error.Validation(
        "Asignacion.FechaRetornoInvalida",
        "La fecha de retorno no puede ser anterior a la fecha de salida.");

    public static Error AutoAprobacion => Error.Forbidden(
        "Asignacion.AutoAprobacion",
        "No puede aprobar una asignación que usted registró; debe aprobarla otra persona.");

    /// <summary>Otra asignación aprobada del vehículo coincide con el período (incluida la reserva previa).</summary>
    public static Error VehiculoOcupado(string placa, string asignadoA, DateTime salida, DateTime retornoEstimado) => Error.Conflict(
        "Asignacion.VehiculoOcupado",
        $"El vehículo {placa} ya está asignado a {asignadoA} del {Fecha(salida)} al {Fecha(retornoEstimado)}. " +
        $"Un vehículo queda reservado desde {Entities.Historico.Asignacion.HorasReservaPrevia} horas antes de cada salida. " +
        "Elija otro vehículo u otro período.");

    /// <summary>Una asignación aprobada del vehículo pasó su retorno estimado sin registrar la entrada.</summary>
    public static Error RetornoPendiente(string placa, string asignadoA, DateTime retornoEstimado) => Error.Conflict(
        "Asignacion.RetornoPendiente",
        $"El vehículo {placa} no ha regresado de la asignación de {asignadoA}, cuyo retorno estaba previsto para el " +
        $"{Fecha(retornoEstimado)}. Registre su retorno antes de asignarlo de nuevo.");

    public static Error ChoferOcupado(string chofer, string placa, DateTime salida, DateTime retornoEstimado) => Error.Conflict(
        "Asignacion.ChoferOcupado",
        $"El chofer {chofer} ya conduce el vehículo {placa} del {Fecha(salida)} al {Fecha(retornoEstimado)}. " +
        "Elija otro chofer u otro período.");

    public static Error RetornoVencido(DateTime retornoEstimado) => Error.Conflict(
        "Asignacion.RetornoVencido",
        $"El retorno estimado ({Fecha(retornoEstimado)}) ya pasó, por lo que la asignación no se puede aprobar. " +
        "Recházela y registre una nueva con las fechas correctas.");

    /// <summary>Hay advertencias (ej. mantenimiento pendiente) que el usuario debe confirmar para continuar.</summary>
    public static Error RequiereConfirmacion(IEnumerable<string> advertencias) => Error.Conflict(
        "Asignacion.RequiereConfirmacion",
        "Confirme para continuar: " + string.Join(" ", advertencias));
}
