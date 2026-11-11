using Application.Common.Response;
using Application.Contracts;
using Application.Contracts.Usuarios;
using Application.Features.Taller.Solicitudes;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Constants;
using Domain.Entities.Combustible;
using Domain.Entities.Historico;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Tickets;

/// <summary>Ticket de una solicitud con los datos de su uso (fecha, operador y quién recibió).</summary>
public class TicketDetalleViewModel
{
    public Guid Id { get; set; }
    public required string Numero { get; set; }
    public required string TipoCombustible { get; set; }
    public decimal Galones { get; set; }
    public required string Estatus { get; set; }
    public DateTime? FechaUso { get; set; }
    public string? DespachadoPor { get; set; }
    public string? ReceptorNombre { get; set; }
}

/// <summary>Totales de la solicitud por tipo de combustible.</summary>
public class ResumenCombustibleViewModel
{
    public required string TipoCombustible { get; set; }
    public int Tickets { get; set; }
    public decimal Galones { get; set; }
    public int TicketsUtilizados { get; set; }
    public decimal GalonesUtilizados { get; set; }
    /// <summary>Tickets disponibles o reservados: aún pueden canjearse.</summary>
    public int TicketsPendientes { get; set; }
    public decimal GalonesPendientes { get; set; }
    public int TicketsAnulados { get; set; }
}

public class AdjuntoSolicitudTicketViewModel
{
    public Guid Id { get; set; }
    public required string NombreArchivo { get; set; }
    public required string ContentType { get; set; }
    public string? TipoDocumento { get; set; }
}

public class SolicitudTicketDetalleViewModel
{
    public Guid Id { get; set; }
    public required string Estatus { get; set; }
    public required string TipoBeneficiario { get; set; }
    /// <summary>Cargo, o apellido y nombre de la persona a la que se asignaron los tickets.</summary>
    public string? EntregadoA { get; set; }
    public string? EntregadoACedula { get; set; }
    public string? EntregadoARango { get; set; }
    public required string Solicitante { get; set; }
    public required string SolicitanteCedula { get; set; }
    public required string CargoSolicitante { get; set; }
    public required string DependenciaSolicitante { get; set; }
    public required string Proposito { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public string? RegistradoPor { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? AprobadoPor { get; set; }
    public string? MotivoRechazoCancelacion { get; set; }
    public decimal TotalGalones { get; set; }
    public decimal GalonesUtilizados { get; set; }
    public List<ResumenCombustibleViewModel> Resumen { get; set; } = [];
    public List<TicketDetalleViewModel> Tickets { get; set; } = [];
    public List<AdjuntoSolicitudTicketViewModel> Adjuntos { get; set; } = [];
}

public record GetSolicitudTicketDetalleQuery(Guid Id) : IRequest<Result<SolicitudTicketDetalleViewModel>>;

/// <summary>Documento adjunto a una solicitud de tickets.</summary>
public record GetAdjuntoSolicitudTicketQuery(Guid Id) : IRequest<Result<ArchivoAdjunto>>;

public class GetSolicitudTicketDetalleQueryHandler(IUnitOfWork uow, IUsuarioService usuarios) :
    IRequestHandler<GetSolicitudTicketDetalleQuery, Result<SolicitudTicketDetalleViewModel>>,
    IRequestHandler<GetAdjuntoSolicitudTicketQuery, Result<ArchivoAdjunto>>
{
    public async Task<Result<SolicitudTicketDetalleViewModel>> Handle(GetSolicitudTicketDetalleQuery request, CancellationToken cancellationToken)
    {
        var s = await uow.Query<SolicitudTicket>()
            .Where(x => x.Id == request.Id && x.Status)
            .Select(x => new
            {
                x.Id,
                x.Estatus,
                x.TipoBeneficiario,
                x.Cargo,
                Persona = x.Persona != null
                    ? new
                    {
                        Nombre = x.Persona.Apellido + " " + x.Persona.Nombre,
                        x.Persona.Identificacion,
                        Rango = x.Persona.Rango != null ? x.Persona.Rango.Nombre : null
                    }
                    : null,
                Solicitante = x.SolicitantePersona!.Apellido + " " + x.SolicitantePersona.Nombre,
                SolicitanteCedula = x.SolicitantePersona.Identificacion,
                x.CargoSolicitante,
                x.DependenciaSolicitante,
                x.Proposito,
                x.CreatedAt,
                x.CreatedBy,
                x.FechaAprobacion,
                x.AprobadoPor,
                x.MotivoRechazoCancelacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (s is null) return Result.Failure<SolicitudTicketDetalleViewModel>(SolicitudTicketErrors.NotFound(request.Id));

        var tickets = await uow.Query<TicketCombustible>()
            .Where(t => t.SolicitudTicketId == s.Id)
            .OrderBy(t => t.Numero)
            .Select(t => new
            {
                t.Id,
                t.Numero,
                t.TipoCombustible,
                t.Galones,
                t.Estatus,
                t.FechaUso,
                t.DespachadoPor
            })
            .ToListAsync(cancellationToken);

        // Canje vigente de cada ticket: quién recibió y, en canjes anteriores al despacho por
        // escaneo, el usuario que lo registró (no se guardaba el operador en el ticket).
        var ticketIds = tickets.Select(t => t.Id).ToList();
        var canjes = (await uow.Query<MovimientoCombustible>()
                .Where(m => m.Status && m.TicketCombustibleId != null && ticketIds.Contains(m.TicketCombustibleId.Value))
                .Select(m => new
                {
                    TicketId = m.TicketCombustibleId!.Value,
                    m.CreatedBy,
                    Receptor = m.ReceptorNombre ?? (m.ReceptorPersona != null
                        ? m.ReceptorPersona.Apellido + " " + m.ReceptorPersona.Nombre
                        : null)
                })
                .ToListAsync(cancellationToken))
            .GroupBy(m => m.TicketId)
            .ToDictionary(g => g.Key, g => g.First());

        var adjuntos = await uow.Query<Adjunto>()
            .Where(a => a.TipoOperacionId == TiposOperacion.SolicitudTicket && a.IdentificadorOperacion == s.Id)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AdjuntoSolicitudTicketViewModel
            {
                Id = a.Id,
                NombreArchivo = a.NombreArchivo,
                ContentType = a.ContentType,
                TipoDocumento = a.TipoDocumento != null ? a.TipoDocumento.Nombre : null
            })
            .ToListAsync(cancellationToken);

        Guid? Operador(Guid? despachadoPor, Guid? registradoPor) => despachadoPor ?? registradoPor;

        var ids = tickets
            .Where(t => t.Estatus == EstatusTicket.Utilizado)
            .Select(t => Operador(t.DespachadoPor, canjes.GetValueOrDefault(t.Id)?.CreatedBy))
            .Append(s.CreatedBy)
            .Append(s.AprobadoPor)
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var nombres = await usuarios.GetNombresAsync(ids, cancellationToken);
        string? Nombre(Guid? id) => id is { } valor ? nombres.GetValueOrDefault(valor) : null;

        var detalle = tickets.Select(t =>
        {
            var utilizado = t.Estatus == EstatusTicket.Utilizado;
            return new TicketDetalleViewModel
            {
                Id = t.Id,
                Numero = t.Numero,
                TipoCombustible = t.TipoCombustible.ToString(),
                Galones = t.Galones,
                Estatus = t.Estatus.ToString(),
                FechaUso = t.FechaUso,
                DespachadoPor = utilizado ? Nombre(Operador(t.DespachadoPor, canjes.GetValueOrDefault(t.Id)?.CreatedBy)) : null,
                ReceptorNombre = utilizado ? canjes.GetValueOrDefault(t.Id)?.Receptor : null
            };
        }).ToList();

        var resumen = tickets
            .GroupBy(t => t.TipoCombustible)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var utilizados = g.Where(t => t.Estatus == EstatusTicket.Utilizado).ToList();
                var pendientes = g.Where(t => t.Estatus is EstatusTicket.Disponible or EstatusTicket.Reservado).ToList();
                return new ResumenCombustibleViewModel
                {
                    TipoCombustible = g.Key.ToString(),
                    Tickets = g.Count(),
                    Galones = g.Sum(t => t.Galones),
                    TicketsUtilizados = utilizados.Count,
                    GalonesUtilizados = utilizados.Sum(t => t.Galones),
                    TicketsPendientes = pendientes.Count,
                    GalonesPendientes = pendientes.Sum(t => t.Galones),
                    TicketsAnulados = g.Count(t => t.Estatus == EstatusTicket.Anulado)
                };
            })
            .ToList();

        return Result.Success(new SolicitudTicketDetalleViewModel
        {
            Id = s.Id,
            Estatus = s.Estatus.ToString(),
            TipoBeneficiario = s.TipoBeneficiario.ToString(),
            EntregadoA = s.Cargo ?? s.Persona?.Nombre,
            EntregadoACedula = s.Persona?.Identificacion,
            EntregadoARango = s.Persona?.Rango,
            Solicitante = s.Solicitante,
            SolicitanteCedula = s.SolicitanteCedula,
            CargoSolicitante = s.CargoSolicitante,
            DependenciaSolicitante = s.DependenciaSolicitante,
            Proposito = s.Proposito,
            FechaSolicitud = s.CreatedAt,
            RegistradoPor = Nombre(s.CreatedBy),
            FechaAprobacion = s.FechaAprobacion,
            AprobadoPor = Nombre(s.AprobadoPor),
            MotivoRechazoCancelacion = s.MotivoRechazoCancelacion,
            TotalGalones = tickets.Sum(t => t.Galones),
            GalonesUtilizados = resumen.Sum(r => r.GalonesUtilizados),
            Resumen = resumen,
            Tickets = detalle,
            Adjuntos = adjuntos
        });
    }

    public async Task<Result<ArchivoAdjunto>> Handle(GetAdjuntoSolicitudTicketQuery request, CancellationToken cancellationToken)
    {
        var archivo = await uow.Query<Adjunto>()
            .Where(a => a.Id == request.Id && a.TipoOperacionId == TiposOperacion.SolicitudTicket)
            .Select(a => new ArchivoAdjunto(a.File, a.ContentType, a.NombreArchivo))
            .FirstOrDefaultAsync(cancellationToken);

        return archivo is null
            ? Result.Failure<ArchivoAdjunto>(Error.NotFound("Adjunto.NotFound", $"No se encontró el adjunto con id '{request.Id}'."))
            : Result.Success(archivo);
    }
}
