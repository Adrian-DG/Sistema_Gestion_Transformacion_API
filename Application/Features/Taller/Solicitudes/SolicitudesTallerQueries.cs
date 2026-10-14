using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Application.Contracts.Usuarios;
using Application.Features.Taller.Inventario;
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

public class SolicitudTallerViewModel
{
    public Guid Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;

    public Guid VehiculoId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string? Ficha { get; set; }
    public string Vehiculo { get; set; } = string.Empty;

    public string Solicitante { get; set; } = string.Empty;
    public string SolicitanteCedula { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public int? Kilometraje { get; set; }
    public DateTime FechaSolicitud { get; set; }

    public Guid? MecanicoId { get; set; }
    public string? Mecanico { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public DateTime? FechaSalida { get; set; }

    public string? Diagnostico { get; set; }
    public string? TrabajoRealizado { get; set; }
    public bool ProcedeBaja { get; set; }
    public string? MotivoRechazoCancelacion { get; set; }

    /// <summary>Costo de las piezas utilizadas (sin las devueltas al cancelar).</summary>
    public decimal CostoPiezas { get; set; }
}

/// <summary>
/// Listado paginado, la más reciente primero. <see cref="PaginationFilterQuery{T}.Param"/> busca por número,
/// placa, ficha, motivo, solicitante o mecánico.
/// </summary>
public record FiltroSolicitudesTallerQuery : PaginationFilterQuery<SolicitudTallerViewModel>
{
    public EstatusSolicitudTaller? Estatus { get; set; }
    public TipoSolicitudTaller? Tipo { get; set; }
    public Guid? VehiculoId { get; set; }
    public Guid? MecanicoId { get; set; }
}

public class FiltroSolicitudesTallerQueryValidator : AbstractValidator<FiltroSolicitudesTallerQuery>
{
    public FiltroSolicitudesTallerQueryValidator()
    {
        Include(new PaginationFilterQueryValidator<SolicitudTallerViewModel>());
        RuleFor(x => x.Estatus).IsInEnum();
        RuleFor(x => x.Tipo).IsInEnum();
    }
}

internal static class SolicitudesTallerProyeccion
{
    public static IQueryable<SolicitudTallerViewModel> AViewModel(this IQueryable<SolicitudTaller> query) =>
        query.Select(s => new SolicitudTallerViewModel
        {
            Id = s.Id,
            Numero = s.Numero,
            Tipo = s.Tipo.ToString(),
            Estatus = s.Estatus.ToString(),
            VehiculoId = s.VehiculoId,
            Placa = s.Vehiculo!.Placa,
            Ficha = s.Vehiculo.Ficha,
            Vehiculo = s.Vehiculo.Marca!.Nombre + " " + s.Vehiculo.Modelo!.Nombre + " (" + s.Vehiculo.Fabricacion + ")",
            Solicitante = s.SolicitantePersona!.Apellido + " " + s.SolicitantePersona.Nombre,
            SolicitanteCedula = s.SolicitantePersona.Identificacion,
            Motivo = s.Motivo,
            Kilometraje = s.Kilometraje,
            FechaSolicitud = s.CreatedAt,
            MecanicoId = s.MecanicoId,
            Mecanico = s.Mecanico != null ? s.Mecanico.Persona!.Apellido + " " + s.Mecanico.Persona.Nombre : null,
            FechaIngreso = s.FechaIngreso,
            FechaSalida = s.FechaSalida,
            Diagnostico = s.Diagnostico,
            TrabajoRealizado = s.TrabajoRealizado,
            ProcedeBaja = s.ProcedeBaja,
            MotivoRechazoCancelacion = s.MotivoRechazoCancelacion,
            CostoPiezas = s.Movimientos
                .Where(m => m.Status && (m.Concepto == ConceptoMovimientoPieza.Consumo || m.Concepto == ConceptoMovimientoPieza.Devolucion))
                .Sum(m => (m.Concepto == ConceptoMovimientoPieza.Consumo ? 1 : -1) * m.Cantidad * m.CostoUnitario)
        });
}

public class GetSolicitudesTallerQueryHandler(IUnitOfWork uow)
    : IRequestHandler<FiltroSolicitudesTallerQuery, Result<PagedData<SolicitudTallerViewModel>>>
{
    public async Task<Result<PagedData<SolicitudTallerViewModel>>> Handle(FiltroSolicitudesTallerQuery request, CancellationToken cancellationToken)
    {
        var query = uow.Query<SolicitudTaller>();

        if (request.Estatus is { } estatus) query = query.Where(s => s.Estatus == estatus);
        if (request.Tipo is { } tipo) query = query.Where(s => s.Tipo == tipo);
        if (request.VehiculoId is { } vehiculoId) query = query.Where(s => s.VehiculoId == vehiculoId);
        if (request.MecanicoId is { } mecanicoId) query = query.Where(s => s.MecanicoId == mecanicoId);

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            var texto = request.Param.Trim();
            query = query.Where(s =>
                s.Numero.Contains(texto) ||
                s.Motivo.Contains(texto) ||
                s.Vehiculo!.Placa.Contains(texto) ||
                (s.Vehiculo.Ficha != null && s.Vehiculo.Ficha.Contains(texto)) ||
                (s.SolicitantePersona!.Nombre + " " + s.SolicitantePersona.Apellido).Contains(texto) ||
                (s.Mecanico != null && (s.Mecanico.Persona!.Nombre + " " + s.Mecanico.Persona.Apellido).Contains(texto)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AViewModel()
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<SolicitudTallerViewModel>(rows, request.PageSize, request.PageNumber, total));
    }
}

public class SolicitudTallerDetalleViewModel
{
    public required SolicitudTallerViewModel Solicitud { get; set; }
    /// <summary>Piezas utilizadas (consumos); las de compra directa traen proveedor y factura.</summary>
    public List<PiezaUtilizadaViewModel> Piezas { get; set; } = [];
    /// <summary>Compras directas de la orden con su recibo.</summary>
    public List<CompraDirectaViewModel> Compras { get; set; } = [];
    /// <summary>Piezas devueltas al inventario al cancelar la orden.</summary>
    public List<PiezaUtilizadaViewModel> Devoluciones { get; set; } = [];
}

public class PiezaUtilizadaViewModel
{
    public Guid PiezaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Pieza { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    /// <summary>Inventario o CompraDirecta.</summary>
    public string Origen { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public class CompraDirectaViewModel
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Pieza { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? NumeroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public List<AdjuntoResumenViewModel> Recibos { get; set; } = [];
}

public record GetSolicitudTallerQuery(Guid Id) : IRequest<Result<SolicitudTallerDetalleViewModel>>;

public class GetSolicitudTallerQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetSolicitudTallerQuery, Result<SolicitudTallerDetalleViewModel>>
{
    public async Task<Result<SolicitudTallerDetalleViewModel>> Handle(GetSolicitudTallerQuery request, CancellationToken cancellationToken)
    {
        var solicitud = await uow.Query<SolicitudTaller>().Where(s => s.Id == request.Id).AViewModel().FirstOrDefaultAsync(cancellationToken);
        if (solicitud is null) return Result.Failure<SolicitudTallerDetalleViewModel>(TallerErrors.NotFound(request.Id));

        var movimientos = await uow.Query<MovimientoPieza>()
            .Where(m => m.SolicitudTallerId == request.Id && m.Status)
            .OrderBy(m => m.Fecha).ThenBy(m => m.CreatedAt)
            .Select(m => new MovimientoOrden(
                m.Id, m.Concepto, m.PiezaId, m.Pieza!.Codigo, m.Pieza.Nombre, m.Pieza.UnidadMedida,
                m.Cantidad, m.CostoUnitario, m.Fecha, m.Proveedor != null ? m.Proveedor.Nombre : null, m.NumeroFactura))
            .ToListAsync(cancellationToken);

        var comprasIds = movimientos.Where(m => m.Concepto == ConceptoMovimientoPieza.CompraDirecta).Select(m => m.Id).ToList();
        var recibos = await uow.Query<Adjunto>()
            .Where(a => a.TipoOperacionId == TiposOperacion.CompraPieza && a.Status && comprasIds.Contains(a.IdentificadorOperacion))
            .Select(a => new { a.IdentificadorOperacion, Adjunto = new AdjuntoResumenViewModel { Id = a.Id, NombreArchivo = a.NombreArchivo, ContentType = a.ContentType } })
            .ToListAsync(cancellationToken);

        return Result.Success(new SolicitudTallerDetalleViewModel
        {
            Solicitud = solicitud,
            Piezas = movimientos.Where(m => m.Concepto == ConceptoMovimientoPieza.Consumo).Select(m => m.ALinea()).ToList(),
            Devoluciones = movimientos.Where(m => m.Concepto == ConceptoMovimientoPieza.Devolucion).Select(m => m.ALinea()).ToList(),
            Compras = movimientos
                .Where(m => m.Concepto == ConceptoMovimientoPieza.CompraDirecta)
                .Select(m => new CompraDirectaViewModel
                {
                    Id = m.Id,
                    Codigo = m.Codigo,
                    Pieza = m.Pieza,
                    Cantidad = m.Cantidad,
                    CostoUnitario = m.CostoUnitario,
                    Proveedor = m.Proveedor ?? string.Empty,
                    NumeroFactura = m.NumeroFactura,
                    Fecha = m.Fecha,
                    Recibos = recibos.Where(r => r.IdentificadorOperacion == m.Id).Select(r => r.Adjunto).ToList()
                })
                .ToList()
        });
    }

    private record MovimientoOrden(
        Guid Id, ConceptoMovimientoPieza Concepto, Guid PiezaId, string Codigo, string Pieza, string UnidadMedida,
        decimal Cantidad, decimal CostoUnitario, DateTime Fecha, string? Proveedor, string? NumeroFactura)
    {
        public PiezaUtilizadaViewModel ALinea() => new()
        {
            PiezaId = PiezaId,
            Codigo = Codigo,
            Pieza = Pieza,
            UnidadMedida = UnidadMedida,
            Cantidad = Cantidad,
            CostoUnitario = CostoUnitario,
            Origen = Proveedor != null ? "CompraDirecta" : "Inventario",
            Fecha = Fecha
        };
    }
}

public class HistorialMantenimientosViewModel
{
    /// <summary>Todas las solicitudes de taller del vehículo, la más reciente primero.</summary>
    public List<MantenimientoViewModel> Solicitudes { get; set; } = [];
    public List<ReactivacionViewModel> Reactivaciones { get; set; } = [];
}

public class MantenimientoViewModel
{
    public required SolicitudTallerViewModel Solicitud { get; set; }
    public List<PiezaUtilizadaViewModel> Piezas { get; set; } = [];
}

public class ReactivacionViewModel
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string AutorizadoPor { get; set; } = string.Empty;
}

/// <summary>Historial de mantenimientos del vehículo: sus entradas al taller con las piezas utilizadas y sus reactivaciones.</summary>
public record GetHistorialMantenimientosQuery(Guid VehiculoId) : IRequest<Result<HistorialMantenimientosViewModel>>;

public class GetHistorialMantenimientosQueryHandler(IUnitOfWork uow, IUsuarioService usuarios)
    : IRequestHandler<GetHistorialMantenimientosQuery, Result<HistorialMantenimientosViewModel>>
{
    public async Task<Result<HistorialMantenimientosViewModel>> Handle(GetHistorialMantenimientosQuery request, CancellationToken cancellationToken)
    {
        if (!await uow.Query<Domain.Entities.Recursos.Vehiculo>().AnyAsync(v => v.Id == request.VehiculoId, cancellationToken))
        {
            return Result.Failure<HistorialMantenimientosViewModel>(VehiculoErrors.NotFound(request.VehiculoId));
        }

        var solicitudes = await uow.Query<SolicitudTaller>()
            .Where(s => s.VehiculoId == request.VehiculoId)
            .OrderByDescending(s => s.CreatedAt)
            .AViewModel()
            .ToListAsync(cancellationToken);

        var ids = solicitudes.Select(s => s.Id).ToList();
        var consumos = await uow.Query<MovimientoPieza>()
            .Where(m => m.SolicitudTallerId != null && ids.Contains(m.SolicitudTallerId.Value) && m.Status &&
                m.Concepto == ConceptoMovimientoPieza.Consumo)
            .OrderBy(m => m.Fecha)
            .Select(m => new
            {
                SolicitudId = m.SolicitudTallerId!.Value,
                Linea = new PiezaUtilizadaViewModel
                {
                    PiezaId = m.PiezaId,
                    Codigo = m.Pieza!.Codigo,
                    Pieza = m.Pieza.Nombre,
                    UnidadMedida = m.Pieza.UnidadMedida,
                    Cantidad = m.Cantidad,
                    CostoUnitario = m.CostoUnitario,
                    Origen = m.ProveedorId != null ? "CompraDirecta" : "Inventario",
                    Fecha = m.Fecha
                }
            })
            .ToListAsync(cancellationToken);

        var reactivaciones = await uow.Query<ReactivacionVehiculo>()
            .Where(r => r.VehiculoId == request.VehiculoId && r.Status)
            .OrderByDescending(r => r.Fecha)
            .Select(r => new { r.Id, r.Fecha, r.Motivo, r.CreatedBy })
            .ToListAsync(cancellationToken);

        var nombres = await usuarios.GetNombresAsync(reactivaciones.Select(r => r.CreatedBy).Distinct().ToList(), cancellationToken);

        return Result.Success(new HistorialMantenimientosViewModel
        {
            Solicitudes = solicitudes.Select(s => new MantenimientoViewModel
            {
                Solicitud = s,
                Piezas = consumos.Where(c => c.SolicitudId == s.Id).Select(c => c.Linea).ToList()
            }).ToList(),
            Reactivaciones = reactivaciones.Select(r => new ReactivacionViewModel
            {
                Id = r.Id,
                Fecha = r.Fecha,
                Motivo = r.Motivo,
                AutorizadoPor = nombres.GetValueOrDefault(r.CreatedBy, string.Empty)
            }).ToList()
        });
    }
}

/// <summary>Archivo adjunto de una entrada de piezas o de una compra directa del taller.</summary>
public record GetAdjuntoTallerQuery(Guid Id) : IRequest<Result<ArchivoAdjunto>>;

public record ArchivoAdjunto(byte[] Contenido, string ContentType, string NombreArchivo);

public class GetAdjuntoTallerQueryHandler(IUnitOfWork uow) : IRequestHandler<GetAdjuntoTallerQuery, Result<ArchivoAdjunto>>
{
    public async Task<Result<ArchivoAdjunto>> Handle(GetAdjuntoTallerQuery request, CancellationToken cancellationToken)
    {
        var archivo = await uow.Query<Adjunto>()
            .Where(a => a.Id == request.Id && a.Status &&
                (a.TipoOperacionId == TiposOperacion.EntradaPieza || a.TipoOperacionId == TiposOperacion.CompraPieza))
            .Select(a => new ArchivoAdjunto(a.File, a.ContentType, a.NombreArchivo))
            .FirstOrDefaultAsync(cancellationToken);

        return archivo is null
            ? Result.Failure<ArchivoAdjunto>(Error.NotFound("Adjunto.NotFound", $"No se encontró el adjunto con id '{request.Id}'."))
            : Result.Success(archivo);
    }
}
