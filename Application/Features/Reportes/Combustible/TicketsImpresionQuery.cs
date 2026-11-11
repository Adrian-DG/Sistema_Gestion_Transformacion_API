using Application.Contracts;
using Application.Contracts.Reportes;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Combustible;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reportes.Combustible;

/// <summary>
/// PDF con los tickets disponibles de una solicitud aprobada, cada uno con su código de barras para
/// escanearlo en el despacho. Los ya utilizados no se vuelven a imprimir.
/// </summary>
public record GetTicketsImpresionQuery(Guid SolicitudTicketId) : IRequest<Result<ArchivoReporte>>;

public class GetTicketsImpresionQueryHandler(IUnitOfWork uow, IMediator mediator, IPdfReportGenerator pdf)
    : IRequestHandler<GetTicketsImpresionQuery, Result<ArchivoReporte>>
{
    public async Task<Result<ArchivoReporte>> Handle(GetTicketsImpresionQuery request, CancellationToken cancellationToken)
    {
        var s = await uow.Query<SolicitudTicket>()
            .Where(x => x.Id == request.SolicitudTicketId && x.Status)
            .Select(x => new
            {
                x.Estatus,
                x.Cargo,
                Persona = x.Persona != null ? x.Persona.Nombre + " " + x.Persona.Apellido : null,
                Rango = x.Persona != null && x.Persona.Rango != null ? x.Persona.Rango.Nombre : null,
                EntregadoACedula = x.Persona != null ? x.Persona.Identificacion : null,
                x.Proposito,
                x.FechaAprobacion,
                Tickets = x.Tickets
                    .Where(t => t.Estatus == EstatusTicket.Disponible)
                    .OrderBy(t => t.Numero)
                    .Select(t => new { t.Numero, t.CodigoVerificacion, t.TipoCombustible, t.Galones })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (s is null) return Result.Failure<ArchivoReporte>(SolicitudTicketErrors.NotFound(request.SolicitudTicketId));
        if (s.Estatus != EstatusSolicitud.Aprobada || s.Tickets.Count == 0)
        {
            return Result.Failure<ArchivoReporte>(SolicitudTicketErrors.NoImprimible);
        }

        var datos = new TicketsImpresion(
            s.Cargo ?? (s.Rango is null ? s.Persona : $"{s.Rango} {s.Persona}") ?? "—",
            s.EntregadoACedula,
            s.Proposito,
            s.FechaAprobacion,
            s.Tickets.Select(t => new TicketImpresion(
                t.Numero,
                t.CodigoVerificacion is null ? t.Numero : $"{t.Numero}-{t.CodigoVerificacion}",
                t.CodigoVerificacion,
                t.TipoCombustible == TipoCombustible.Gasoil ? "Gasoil" : "Gasolina",
                t.Galones)).ToList(),
            DateTime.Now);

        var encabezado = await EncabezadoPdf.ObtenerAsync(mediator, uow, TipoReportePdf.TicketsCombustible, cancellationToken);
        var contenido = pdf.TicketsCombustible(encabezado, datos);
        var nombre = s.Tickets.Count == 1
            ? $"Ticket_{s.Tickets[0].Numero}.pdf"
            : $"Tickets_{s.Tickets[0].Numero}_a_{s.Tickets[^1].Numero}.pdf";
        return Result.Success(new ArchivoReporte(contenido, nombre));
    }
}
