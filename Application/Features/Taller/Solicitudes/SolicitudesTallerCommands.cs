using Application.Common.DTO;
using Application.Common.Validation;
using Application.Contracts;
using Application.Features.Taller.Inventario;
using Application.Features.Vehiculo;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Constants;
using Domain.Entities.Historico;
using Domain.Entities.Taller;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Taller.Solicitudes;

/// <summary>
/// Registra la solicitud de taller (Pendiente). El vehículo sigue en su estatus hasta el ingreso; no
/// puede estar inactivo ni tener otra solicitud abierta. El número (ST-año-secuencia) lo genera la API.
/// Se atiende en el taller interno o en un taller autorizado (<paramref name="TallerExternoId"/>), por
/// ejemplo el concesionario de un vehículo en garantía. El kilometraje es obligatorio y no puede ser
/// menor que el último registrado del vehículo.
/// </summary>
public record CreateSolicitudTallerCommand(
    Guid VehiculoId,
    TipoSolicitudTaller Tipo,
    Guid SolicitantePersonaId,
    string Motivo,
    int Kilometraje,
    LugarMantenimiento Lugar = LugarMantenimiento.Interno,
    Guid? TallerExternoId = null
) : IRequest<Result<SolicitudTallerCreadaViewModel>>;

public record SolicitudTallerCreadaViewModel(Guid Id, string Numero);

public class CreateSolicitudTallerCommandValidator : AbstractValidator<CreateSolicitudTallerCommand>
{
    public CreateSolicitudTallerCommandValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty().WithMessage("El vehículo es obligatorio.");
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.SolicitantePersonaId).NotEmpty().WithMessage("El solicitante es obligatorio.");
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Kilometraje).GreaterThanOrEqualTo(0).WithMessage("Indique el kilometraje del vehículo.");
        RuleFor(x => x.Lugar).IsInEnum();
        RuleFor(x => x.TallerExternoId).NotEmpty()
            .When(x => x.Lugar == LugarMantenimiento.Externo)
            .WithMessage("Seleccione el taller autorizado.");
        RuleFor(x => x.TallerExternoId).Null()
            .When(x => x.Lugar == LugarMantenimiento.Interno)
            .WithMessage("Una solicitud del taller interno no lleva taller externo.");
        RuleFor(x => x.Lugar).Equal(LugarMantenimiento.Interno)
            .When(x => x.Tipo == TipoSolicitudTaller.EvaluacionBaja)
            .WithMessage("La evaluación de baja la hace un mecánico del taller interno.");
    }
}

/// <summary>
/// El vehículo entra al taller y pasa a En mantenimiento. En el taller interno se asigna un mecánico;
/// en uno externo, no. El kilometraje del ingreso actualiza el del vehículo.
/// </summary>
public record IngresarSolicitudTallerCommand(Guid SolicitudId, Guid? MecanicoId, int Kilometraje) : IRequest<Result<IngresoTallerViewModel>>;

/// <param name="AsignacionesAprobadas">
/// Asignaciones ya aprobadas con salida futura: el vehículo no podrá cumplirlas mientras esté en el
/// taller; hay que reprogramarlas o cancelarlas.
/// </param>
public record IngresoTallerViewModel(int AsignacionesAprobadas);

public class IngresarSolicitudTallerCommandValidator : AbstractValidator<IngresarSolicitudTallerCommand>
{
    public IngresarSolicitudTallerCommandValidator()
    {
        RuleFor(x => x.Kilometraje).GreaterThanOrEqualTo(0).WithMessage("Indique el kilometraje del vehículo.");
    }
}

/// <summary>Datos de la compra de las unidades que faltan en inventario. El recibo es obligatorio.</summary>
public record CompraPiezaRequest(Guid ProveedorId, decimal CostoUnitario, string? NumeroFactura, AdjuntoRequest Recibo);

/// <summary>
/// Registra piezas utilizadas en la orden. Se toma lo que haya en inventario; si no alcanza, las
/// faltantes se compran: hay que indicar <paramref name="Compra"/> con su costo y recibo. Sin compra,
/// la API responde Taller.CompraRequerida con lo disponible y lo faltante.
/// </summary>
public record AgregarPiezaSolicitudTallerCommand(Guid SolicitudId, Guid PiezaId, decimal Cantidad, CompraPiezaRequest? Compra)
    : IRequest<Result<PiezaAgregadaViewModel>>;

public record PiezaAgregadaViewModel(decimal DelInventario, decimal Compradas);

public class AgregarPiezaSolicitudTallerCommandValidator : AbstractValidator<AgregarPiezaSolicitudTallerCommand>
{
    public AgregarPiezaSolicitudTallerCommandValidator()
    {
        RuleFor(x => x.PiezaId).NotEmpty().WithMessage("Seleccione la pieza.");
        RuleFor(x => x.Cantidad).GreaterThan(0).PrecisionScale(12, 2, true);
        When(x => x.Compra is not null, () =>
        {
            RuleFor(x => x.Compra!.ProveedorId).NotEmpty().OverridePropertyName("ProveedorId")
                .WithMessage("Indique a qué proveedor se compró.");
            RuleFor(x => x.Compra!.CostoUnitario).GreaterThan(0).PrecisionScale(14, 2, true).OverridePropertyName("CostoUnitario")
                .WithMessage("Indique el costo unitario de la compra.");
            RuleFor(x => x.Compra!.NumeroFactura).MaximumLength(50).OverridePropertyName("NumeroFactura");
            RuleFor(x => x.Compra!.Recibo).NotNull().OverridePropertyName("Recibo")
                .WithMessage("Adjunte la foto del recibo de compra.")
                .SetValidator(new AdjuntoRequestValidator());
        });
    }
}

/// <summary>
/// Cierra la orden con el diagnóstico y el trabajo realizado. Si el mecánico dictamina la baja
/// (<paramref name="ProcedeBaja"/>), el vehículo pasa a inactivo; si no, vuelve a operativo.
/// </summary>
/// <remarks>
/// En una solicitud externa se registra lo que cobró el taller autorizado (<paramref name="CostoServicio"/>,
/// obligatorio), su factura y la orden de servicio adjunta; no se dictamina baja.
/// </remarks>
public record FinalizarSolicitudTallerCommand(
    Guid SolicitudId,
    string Diagnostico,
    string TrabajoRealizado,
    bool ProcedeBaja,
    decimal? CostoServicio = null,
    string? NumeroFacturaServicio = null,
    List<AdjuntoRequest>? Adjuntos = null
) : IRequest<Result>;

public class FinalizarSolicitudTallerCommandValidator : AbstractValidator<FinalizarSolicitudTallerCommand>
{
    public FinalizarSolicitudTallerCommandValidator()
    {
        RuleFor(x => x.Diagnostico).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TrabajoRealizado).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.CostoServicio).GreaterThanOrEqualTo(0).PrecisionScale(14, 2, true);
        RuleFor(x => x.NumeroFacturaServicio).MaximumLength(50);
        RuleForEach(x => x.Adjuntos).SetValidator(new AdjuntoRequestValidator());
    }
}

public record RechazarSolicitudTallerCommand(Guid SolicitudId, string Motivo) : IRequest<Result>;

/// <summary>Anula la solicitud; si el vehículo estaba en el taller sale y las piezas consumidas vuelven al inventario.</summary>
public record CancelarSolicitudTallerCommand(Guid SolicitudId, string Motivo) : IRequest<Result>;

public class RechazarSolicitudTallerCommandValidator : AbstractValidator<RechazarSolicitudTallerCommand>
{
    public RechazarSolicitudTallerCommandValidator() => RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
}

public class CancelarSolicitudTallerCommandValidator : AbstractValidator<CancelarSolicitudTallerCommand>
{
    public CancelarSolicitudTallerCommandValidator() => RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
}

public class SolicitudTallerCommandsHandler(IUnitOfWork uow) :
    IRequestHandler<CreateSolicitudTallerCommand, Result<SolicitudTallerCreadaViewModel>>,
    IRequestHandler<IngresarSolicitudTallerCommand, Result<IngresoTallerViewModel>>,
    IRequestHandler<AgregarPiezaSolicitudTallerCommand, Result<PiezaAgregadaViewModel>>,
    IRequestHandler<FinalizarSolicitudTallerCommand, Result>,
    IRequestHandler<RechazarSolicitudTallerCommand, Result>,
    IRequestHandler<CancelarSolicitudTallerCommand, Result>
{
    public async Task<Result<SolicitudTallerCreadaViewModel>> Handle(CreateSolicitudTallerCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = await uow.Query<Domain.Entities.Recursos.Vehiculo>()
            .Where(v => v.Id == request.VehiculoId)
            .Select(v => new { v.Placa, v.Estado, v.Kilometraje })
            .FirstOrDefaultAsync(cancellationToken);

        if (vehiculo is null) return Result.Failure<SolicitudTallerCreadaViewModel>(VehiculoErrors.NotFound(request.VehiculoId));
        if (vehiculo.Estado == EstadoVehiculo.Inactivo)
        {
            return Result.Failure<SolicitudTallerCreadaViewModel>(TallerErrors.VehiculoInactivo(vehiculo.Placa));
        }

        if (request.Kilometraje < vehiculo.Kilometraje)
        {
            return Result.Failure<SolicitudTallerCreadaViewModel>(TallerErrors.KilometrajeMenor(vehiculo.Placa, vehiculo.Kilometraje));
        }

        if (request.TallerExternoId is { } tallerId &&
            !await uow.Query<Proveedor>().AnyAsync(p => p.Id == tallerId && p.Status && p.EsTallerAutorizado, cancellationToken))
        {
            return Result.Failure<SolicitudTallerCreadaViewModel>(TallerErrors.TallerExternoInvalido(tallerId));
        }

        var abierta = await uow.Query<SolicitudTaller>()
            .Where(s => s.VehiculoId == request.VehiculoId &&
                (s.Estatus == EstatusSolicitudTaller.Pendiente || s.Estatus == EstatusSolicitudTaller.EnTaller))
            .Select(s => s.Numero)
            .FirstOrDefaultAsync(cancellationToken);
        if (abierta is not null) return Result.Failure<SolicitudTallerCreadaViewModel>(TallerErrors.SolicitudActiva(vehiculo.Placa, abierta));

        var solicitante = await uow.ValidarPersonaAsync(request.SolicitantePersonaId, cancellationToken);
        if (solicitante.IsFailure) return Result.Failure<SolicitudTallerCreadaViewModel>(solicitante.Error);

        // El número se reserva en la transacción que guarda la solicitud: si falla, no se pierde.
        return await uow.EnTransaccionAsync(async () =>
        {
            var anio = DateTime.Now.Year;
            var secuencia = await uow.ReservarSecuenciaAsync(SolicitudTaller.CodigoSecuencia, anio, 1, cancellationToken);

            var solicitud = new SolicitudTaller
            {
                Id = Guid.NewGuid(),
                Numero = SolicitudTaller.FormatearNumero(anio, secuencia),
                Tipo = request.Tipo,
                Lugar = request.Lugar,
                TallerExternoId = request.TallerExternoId,
                Estatus = EstatusSolicitudTaller.Pendiente,
                FechaSolicitud = DateTime.Now,
                VehiculoId = request.VehiculoId,
                SolicitantePersonaId = request.SolicitantePersonaId,
                Motivo = request.Motivo.Trim(),
                Kilometraje = request.Kilometraje,
                Status = true
            };

            await uow.AddAsync(solicitud, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
            return Result.Success(new SolicitudTallerCreadaViewModel(solicitud.Id, solicitud.Numero));
        }, cancellationToken);
    }

    public async Task<Result<IngresoTallerViewModel>> Handle(IngresarSolicitudTallerCommand request, CancellationToken cancellationToken)
    {
        var solicitud = await uow.Tracked<SolicitudTaller>()
            .Include(s => s.Vehiculo)
            .FirstOrDefaultAsync(s => s.Id == request.SolicitudId, cancellationToken);
        if (solicitud is null) return Result.Failure<IngresoTallerViewModel>(TallerErrors.NotFound(request.SolicitudId));

        var vehiculo = solicitud.Vehiculo!;
        if (vehiculo.Estado == EstadoVehiculo.Inactivo)
        {
            return Result.Failure<IngresoTallerViewModel>(TallerErrors.VehiculoInactivo(vehiculo.Placa));
        }

        if (request.Kilometraje < vehiculo.Kilometraje)
        {
            return Result.Failure<IngresoTallerViewModel>(TallerErrors.KilometrajeMenor(vehiculo.Placa, vehiculo.Kilometraje));
        }

        // El mecánico solo aplica al taller interno; en el externo lo atiende el taller autorizado.
        Guid? mecanicoId = null;
        if (!solicitud.EsExterna)
        {
            if (request.MecanicoId is not { } id || id == Guid.Empty)
            {
                return Result.Failure<IngresoTallerViewModel>(TallerErrors.MecanicoRequerido());
            }
            if (!await uow.Query<Mecanico>().AnyAsync(m => m.Id == id && m.Status, cancellationToken))
            {
                return Result.Failure<IngresoTallerViewModel>(MecanicoErrors.NotFound(id));
            }
            mecanicoId = id;
        }

        var ahora = DateTime.Now;
        var enMision = await uow.Query<Asignacion>()
            .Where(a => a.VehiculoId == vehiculo.Id)
            .AnyAsync(EstatusVehiculos.EnMision(ahora), cancellationToken);
        if (enMision) return Result.Failure<IngresoTallerViewModel>(TallerErrors.VehiculoEnMision(vehiculo.Placa));

        var ingreso = solicitud.Ingresar(mecanicoId, ahora, request.Kilometraje);
        if (ingreso.IsFailure) return Result.Failure<IngresoTallerViewModel>(ingreso.Error);

        vehiculo.Estado = EstadoVehiculo.EnMantenimiento;
        vehiculo.Kilometraje = request.Kilometraje;

        var aprobadas = await uow.Query<Asignacion>()
            .CountAsync(a => a.VehiculoId == vehiculo.Id && a.Estatus == EstatusSolicitud.Aprobada && a.FechaSalida > ahora, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(new IngresoTallerViewModel(aprobadas));
    }

    public async Task<Result<PiezaAgregadaViewModel>> Handle(AgregarPiezaSolicitudTallerCommand request, CancellationToken cancellationToken)
    {
        var solicitud = await uow.Query<SolicitudTaller>()
            .Where(s => s.Id == request.SolicitudId)
            .Select(s => new { s.Numero, s.Estatus, s.Lugar })
            .FirstOrDefaultAsync(cancellationToken);
        if (solicitud is null) return Result.Failure<PiezaAgregadaViewModel>(TallerErrors.NotFound(request.SolicitudId));
        if (solicitud.Lugar == LugarMantenimiento.Externo)
        {
            return Result.Failure<PiezaAgregadaViewModel>(TallerErrors.SoloInterno(solicitud.Numero));
        }
        if (solicitud.Estatus != EstatusSolicitudTaller.EnTaller)
        {
            return Result.Failure<PiezaAgregadaViewModel>(TallerErrors.InvalidStatus(solicitud.Numero, solicitud.Estatus.ToString()));
        }

        var pieza = await uow.Query<Pieza>()
            .Where(p => p.Id == request.PiezaId && p.Status)
            .Select(p => new { p.Nombre })
            .FirstOrDefaultAsync(cancellationToken);
        if (pieza is null) return Result.Failure<PiezaAgregadaViewModel>(InventarioPiezasErrors.PiezaNotFound(request.PiezaId));

        if (request.Compra is { } datosCompra &&
            !await uow.Query<Proveedor>().AnyAsync(p => p.Id == datosCompra.ProveedorId && p.Status, cancellationToken))
        {
            return Result.Failure<PiezaAgregadaViewModel>(InventarioPiezasErrors.ProveedorNotFound(datosCompra.ProveedorId));
        }

        return await uow.EnTransaccionAsync(async () =>
        {
            var (existencia, costoPromedio) = await uow.ObtenerExistenciaAsync(request.PiezaId, cancellationToken);
            var delInventario = Math.Min(Math.Max(existencia, 0), request.Cantidad);
            var faltante = request.Cantidad - delInventario;

            if (faltante > 0 && request.Compra is null)
            {
                return Result.Failure<PiezaAgregadaViewModel>(TallerErrors.CompraRequerida(pieza.Nombre, delInventario, faltante));
            }

            if (faltante == 0 && request.Compra is not null)
            {
                return Result.Failure<PiezaAgregadaViewModel>(TallerErrors.CompraInnecesaria(pieza.Nombre));
            }

            var ahora = DateTime.Now;
            var movimientos = new List<MovimientoPieza>();

            if (delInventario > 0)
            {
                movimientos.Add(Movimiento(ConceptoMovimientoPieza.Consumo, delInventario, costoPromedio, ahora));
            }

            if (faltante > 0)
            {
                var compra = request.Compra!;
                var numeroFactura = string.IsNullOrWhiteSpace(compra.NumeroFactura) ? null : compra.NumeroFactura.Trim();

                // La compra entra al kardex y sale de inmediato a la orden: la existencia no cambia.
                var entrada = Movimiento(ConceptoMovimientoPieza.CompraDirecta, faltante, compra.CostoUnitario, ahora);
                entrada.ProveedorId = compra.ProveedorId;
                entrada.NumeroFactura = numeroFactura;

                var consumo = Movimiento(ConceptoMovimientoPieza.Consumo, faltante, compra.CostoUnitario, ahora);
                consumo.ProveedorId = compra.ProveedorId;
                consumo.NumeroFactura = numeroFactura;

                movimientos.Add(entrada);
                movimientos.Add(consumo);
                await uow.AddAsync(compra.Recibo.ToEntity(TiposOperacion.CompraPieza, entrada.Id), cancellationToken);
            }

            await uow.AddRangeAsync(movimientos, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
            return Result.Success(new PiezaAgregadaViewModel(delInventario, faltante));
        }, cancellationToken);

        MovimientoPieza Movimiento(ConceptoMovimientoPieza concepto, decimal cantidad, decimal costo, DateTime fecha) => new()
        {
            Id = Guid.NewGuid(),
            Concepto = concepto,
            PiezaId = request.PiezaId,
            Cantidad = cantidad,
            CostoUnitario = costo,
            Fecha = fecha,
            SolicitudTallerId = request.SolicitudId,
            Status = true
        };
    }

    public async Task<Result> Handle(FinalizarSolicitudTallerCommand request, CancellationToken cancellationToken)
    {
        var solicitud = await uow.Tracked<SolicitudTaller>()
            .Include(s => s.Vehiculo)
            .FirstOrDefaultAsync(s => s.Id == request.SolicitudId, cancellationToken);
        if (solicitud is null) return Result.Failure(TallerErrors.NotFound(request.SolicitudId));

        Result resultado;
        if (solicitud.EsExterna)
        {
            if (request.ProcedeBaja) return Result.Failure(TallerErrors.SoloInterno(solicitud.Numero));
            if (request.CostoServicio is not { } costo)
            {
                return Result.Failure(Error.Validation(
                    "Taller.CostoServicioRequerido", "Indique lo que cobró el taller autorizado por el servicio."));
            }

            var factura = string.IsNullOrWhiteSpace(request.NumeroFacturaServicio) ? null : request.NumeroFacturaServicio.Trim();
            resultado = solicitud.FinalizarExterna(request.Diagnostico.Trim(), request.TrabajoRealizado.Trim(), costo, factura, DateTime.Now);
            if (resultado.IsSuccess && request.Adjuntos is { Count: > 0 })
            {
                await uow.AddRangeAsync(request.Adjuntos.Select(a => a.ToEntity(TiposOperacion.SolicitudTaller, solicitud.Id)), cancellationToken);
            }
        }
        else
        {
            resultado = solicitud.Finalizar(request.Diagnostico.Trim(), request.TrabajoRealizado.Trim(), request.ProcedeBaja, DateTime.Now);
        }
        if (resultado.IsFailure) return resultado;

        solicitud.Vehiculo!.Estado = request.ProcedeBaja ? EstadoVehiculo.Inactivo : EstadoVehiculo.Operativo;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(RechazarSolicitudTallerCommand request, CancellationToken cancellationToken)
    {
        var solicitud = await uow.Tracked<SolicitudTaller>().FirstOrDefaultAsync(s => s.Id == request.SolicitudId, cancellationToken);
        if (solicitud is null) return Result.Failure(TallerErrors.NotFound(request.SolicitudId));

        var resultado = solicitud.Rechazar(request.Motivo.Trim());
        if (resultado.IsFailure) return resultado;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(CancelarSolicitudTallerCommand request, CancellationToken cancellationToken)
    {
        var solicitud = await uow.Tracked<SolicitudTaller>()
            .Include(s => s.Vehiculo)
            .Include(s => s.Movimientos.Where(m => m.Status))
            .FirstOrDefaultAsync(s => s.Id == request.SolicitudId, cancellationToken);
        if (solicitud is null) return Result.Failure(TallerErrors.NotFound(request.SolicitudId));

        var estabaEnTaller = solicitud.Estatus == EstatusSolicitudTaller.EnTaller;
        var ahora = DateTime.Now;

        var resultado = solicitud.Cancelar(request.Motivo.Trim(), ahora);
        if (resultado.IsFailure) return resultado;

        if (estabaEnTaller)
        {
            solicitud.Vehiculo!.Estado = EstadoVehiculo.Operativo;

            // Las piezas consumidas (del inventario o compradas para la orden) vuelven al inventario.
            var devoluciones = solicitud.Movimientos
                .Where(m => m.Concepto == ConceptoMovimientoPieza.Consumo)
                .Select(m => new MovimientoPieza
                {
                    Concepto = ConceptoMovimientoPieza.Devolucion,
                    PiezaId = m.PiezaId,
                    Cantidad = m.Cantidad,
                    CostoUnitario = m.CostoUnitario,
                    Fecha = ahora,
                    SolicitudTallerId = solicitud.Id,
                    Status = true
                })
                .ToList();
            await uow.AddRangeAsync(devoluciones, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
