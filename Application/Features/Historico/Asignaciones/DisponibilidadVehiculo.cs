using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Entities.Taller;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Application.Features.Historico.Asignaciones;

/// <summary>Algo a revisar antes de asignar; no impide la asignación si el usuario lo confirma.</summary>
/// <param name="Codigo">MantenimientoPendiente, SinPoliza, PolizaVencida o PolizaVencePronto.</param>
public record AdvertenciaAsignacion(string Codigo, string Mensaje);

/// <summary>
/// Reglas para asignar un vehículo, compartidas por el registro y la aprobación.
/// <list type="bullet">
/// <item>Impiden: vehículo en mantenimiento o inactivo; otra asignación aprobada del vehículo que coincida con
/// el período (se reserva desde <see cref="Asignacion.HorasReservaPrevia"/> horas antes de la salida); un
/// retorno sin registrar de una asignación vencida; el chofer ocupado en el mismo período.</item>
/// <item>Advierten (hay que confirmar): solicitud de taller pendiente de ingreso y póliza vencida o que vence
/// durante la asignación.</item>
/// </list>
/// Las asignaciones pendientes no ocupan el vehículo ni al chofer.
/// </summary>
public static class DisponibilidadVehiculo
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    public static async Task<Result> ValidarAsignacionAsync(
        this IUnitOfWork uow,
        Guid vehiculoId,
        Guid choferPersonaId,
        DateTime salida,
        DateTime retornoEstimado,
        Guid? excluirAsignacionId,
        bool advertenciasConfirmadas,
        CancellationToken cancellationToken)
    {
        var ahora = DateTime.Now;

        var vehiculo = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .Where(v => v.Id == vehiculoId)
            .Select(v => new { v.Placa, v.Estado })
            .FirstOrDefaultAsync(cancellationToken);
        if (vehiculo is null) return Result.Failure(VehiculoErrors.NotFound(vehiculoId));

        if (vehiculo.Estado != EstadoVehiculo.Operativo)
        {
            return Result.Failure(VehiculoErrors.NoOperativo(vehiculo.Placa, vehiculo.Estado));
        }

        var conflicto = await uow.Query<Asignacion>()
            .Where(a =>
                a.VehiculoId == vehiculoId &&
                a.Id != excluirAsignacionId &&
                a.Estatus == EstatusSolicitud.Aprobada &&
                a.FechaSalida.AddHours(-Asignacion.HorasReservaPrevia) < retornoEstimado &&
                (a.FechaRetornoEstimada > salida || a.FechaRetornoEstimada < ahora))
            .OrderBy(a => a.FechaSalida)
            .Select(a => new
            {
                a.FechaSalida,
                a.FechaRetornoEstimada,
                AsignadoA = a.Persona!.Nombre + " " + a.Persona.Apellido
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (conflicto is not null)
        {
            // Si ya pasó su retorno y no se registró la entrada, el vehículo sigue fuera.
            return Result.Failure(conflicto.FechaRetornoEstimada < ahora && conflicto.FechaRetornoEstimada <= salida
                ? AsignacionErrors.RetornoPendiente(vehiculo.Placa, conflicto.AsignadoA, conflicto.FechaRetornoEstimada)
                : AsignacionErrors.VehiculoOcupado(vehiculo.Placa, conflicto.AsignadoA, conflicto.FechaSalida, conflicto.FechaRetornoEstimada));
        }

        var choferOcupado = await uow.Query<Asignacion>()
            .Where(a =>
                a.ChoferPersonaId == choferPersonaId &&
                a.Id != excluirAsignacionId &&
                a.Estatus == EstatusSolicitud.Aprobada &&
                a.FechaSalida < retornoEstimado &&
                a.FechaRetornoEstimada > salida)
            .OrderBy(a => a.FechaSalida)
            .Select(a => new
            {
                Chofer = a.ChoferPersona!.Nombre + " " + a.ChoferPersona.Apellido,
                a.Vehiculo!.Placa,
                a.FechaSalida,
                a.FechaRetornoEstimada
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (choferOcupado is not null)
        {
            return Result.Failure(AsignacionErrors.ChoferOcupado(
                choferOcupado.Chofer, choferOcupado.Placa, choferOcupado.FechaSalida, choferOcupado.FechaRetornoEstimada));
        }

        if (!advertenciasConfirmadas)
        {
            var advertencias = await uow.ObtenerAdvertenciasAsync(vehiculoId, salida, retornoEstimado, cancellationToken);
            if (advertencias.Count > 0)
            {
                return Result.Failure(AsignacionErrors.RequiereConfirmacion(advertencias.Select(a => a.Mensaje)));
            }
        }

        return Result.Success();
    }

    /// <summary>Mantenimiento pendiente y estado de la póliza del vehículo para el período.</summary>
    public static async Task<IReadOnlyList<AdvertenciaAsignacion>> ObtenerAdvertenciasAsync(
        this IUnitOfWork uow, Guid vehiculoId, DateTime salida, DateTime retornoEstimado, CancellationToken cancellationToken)
    {
        var advertencias = new List<AdvertenciaAsignacion>();
        var placa = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .Where(v => v.Id == vehiculoId)
            .Select(v => v.Placa)
            .FirstOrDefaultAsync(cancellationToken);
        if (placa is null) return advertencias;

        var taller = await uow.Query<SolicitudTaller>()
            .Where(s => s.VehiculoId == vehiculoId && s.Estatus == EstatusSolicitudTaller.Pendiente)
            .OrderBy(s => s.FechaSolicitud)
            .Select(s => new { s.Numero, s.Tipo, s.FechaSolicitud })
            .FirstOrDefaultAsync(cancellationToken);

        if (taller is not null)
        {
            advertencias.Add(new AdvertenciaAsignacion("MantenimientoPendiente",
                $"El vehículo {placa} tiene la solicitud de taller {taller.Numero} ({NombreTipo(taller.Tipo)}) registrada el " +
                $"{taller.FechaSolicitud.ToString("dd/MM/yyyy", Cultura)} y pendiente de ingreso; si entra al taller durante " +
                "la asignación, no podrá cumplirse."));
        }

        var poliza = await uow.Query<Poliza>()
            .Where(p => p.VehiculoId == vehiculoId && p.Status)
            .OrderByDescending(p => p.FechaVencimiento)
            .Select(p => new { p.Numero, p.FechaVencimiento })
            .FirstOrDefaultAsync(cancellationToken);

        var diaSalida = DateOnly.FromDateTime(salida);
        var diaRetorno = DateOnly.FromDateTime(retornoEstimado);

        if (poliza is null)
        {
            advertencias.Add(new AdvertenciaAsignacion("SinPoliza",
                $"El vehículo {placa} no tiene una póliza de seguro registrada."));
        }
        else if (poliza.FechaVencimiento < diaSalida)
        {
            advertencias.Add(new AdvertenciaAsignacion("PolizaVencida",
                $"La póliza {poliza.Numero} del vehículo {placa} vence el {poliza.FechaVencimiento.ToString("dd/MM/yyyy", Cultura)}, " +
                "antes de la salida: el vehículo circularía sin seguro."));
        }
        else if (poliza.FechaVencimiento < diaRetorno)
        {
            advertencias.Add(new AdvertenciaAsignacion("PolizaVencePronto",
                $"La póliza {poliza.Numero} del vehículo {placa} vence el {poliza.FechaVencimiento.ToString("dd/MM/yyyy", Cultura)}, " +
                "antes del retorno estimado."));
        }

        return advertencias;
    }

    private static string NombreTipo(TipoSolicitudTaller tipo) => tipo switch
    {
        TipoSolicitudTaller.Mantenimiento => "mantenimiento",
        TipoSolicitudTaller.Reparacion => "reparación",
        TipoSolicitudTaller.EvaluacionBaja => "evaluación de baja",
        _ => tipo.ToString()
    };
}
