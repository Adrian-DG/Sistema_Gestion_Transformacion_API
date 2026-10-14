using Domain.Entities.Historico;
using Domain.Enums;

namespace Application.Features.Vehiculo;

/// <summary>
/// Estatus visible del vehículo. El taller registra su <see cref="EstadoVehiculo"/>; si está operativo,
/// queda No disponible mientras tenga una misión en curso: una asignación aprobada cuya salida ya
/// llegó. La misión termina al registrar el retorno (la asignación pasa a Finalizada), así que un
/// vehículo con el retorno estimado vencido sigue No disponible hasta que se registre su entrada.
/// </summary>
public static class EstatusVehiculos
{
    /// <summary>Para proyecciones de EF: <c>v.Asignaciones!.Any(EnMision(ahora))</c>.</summary>
    public static System.Linq.Expressions.Expression<Func<Asignacion, bool>> EnMision(DateTime ahora) =>
        a => a.Estatus == EstatusSolicitud.Aprobada && a.FechaSalida <= ahora;

    public static EstatusVehiculo Resolver(EstadoVehiculo estado, bool enMision) => estado switch
    {
        EstadoVehiculo.Inactivo => EstatusVehiculo.Inactivo,
        EstadoVehiculo.EnMantenimiento => EstatusVehiculo.EnMantenimiento,
        _ => enMision ? EstatusVehiculo.NoDisponible : EstatusVehiculo.Disponible
    };
}
