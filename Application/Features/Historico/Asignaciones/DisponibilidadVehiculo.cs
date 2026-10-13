using Application.Contracts;
using Domain.Entities.Historico;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico.Asignaciones;

public static class DisponibilidadVehiculo
{
    /// <summary>
    /// Indica si el vehículo tiene una asignación aprobada que choca con el período [inicio, fin].
    /// Una asignación aprobada ocupa el vehículo desde <see cref="Asignacion.HorasReservaPrevia"/> horas antes
    /// de su salida hasta su retorno estimado; si ya pasó el retorno estimado y no se ha registrado
    /// la entrada del vehículo, lo sigue ocupando hasta que se registre.
    /// Las asignaciones pendientes no bloquean el vehículo.
    /// </summary>
    public static Task<bool> VehiculoOcupadoAsync(
        this IUnitOfWork uow,
        Guid vehiculoId,
        DateTime inicio,
        DateTime fin,
        Guid? excluirAsignacionId,
        DateTime ahora,
        CancellationToken cancellationToken)
    {
        return uow.Query<Asignacion>().AnyAsync(a =>
            a.VehiculoId == vehiculoId &&
            a.Id != excluirAsignacionId &&
            a.Estatus == EstatusSolicitud.Aprobada &&
            a.FechaSalida.AddHours(-Asignacion.HorasReservaPrevia) < fin &&
            (a.FechaRetornoEstimada > inicio || a.FechaRetornoEstimada < ahora),
            cancellationToken);
    }
}
