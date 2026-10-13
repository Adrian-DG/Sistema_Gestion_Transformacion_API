using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Combustible;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Tickets;

public class TicketViewModel
{
    public Guid Id { get; set; }
    public required string Numero { get; set; }
    public required string TipoCombustible { get; set; }
    public decimal Galones { get; set; }
    public required string Estatus { get; set; }
    public DateTime? FechaUso { get; set; }
    public Guid SolicitudTicketId { get; set; }
    /// <summary>Cargo o persona a quien se entregó el ticket.</summary>
    public string? EntregadoA { get; set; }
}

public class SolicitudTicketViewModel
{
    public Guid Id { get; set; }
    public required string Estatus { get; set; }
    public required string TipoBeneficiario { get; set; }
    public string? EntregadoA { get; set; }
    public required string Solicitante { get; set; }
    public required string DependenciaSolicitante { get; set; }
    public required string Proposito { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? MotivoRechazoCancelacion { get; set; }
    public decimal TotalGalones { get; set; }
    public List<TicketViewModel> Tickets { get; set; } = [];
}

public record GetTicketByNumeroQuery(string Numero) : IRequest<Result<TicketViewModel>>;

public class GetTicketsQueryHandler(IUnitOfWork uow) :
    IRequestHandler<GetTicketByNumeroQuery, Result<TicketViewModel>>,
    IRequestHandler<PaginationFilterQuery<SolicitudTicketViewModel>, Result<PagedData<SolicitudTicketViewModel>>>
{
    public async Task<Result<TicketViewModel>> Handle(GetTicketByNumeroQuery request, CancellationToken cancellationToken)
    {
        var numero = request.Numero.Trim();
        var ticket = await uow.Query<TicketCombustible>()
            .Where(t => t.Numero == numero)
            // Si el número fue anulado y registrado de nuevo, se muestra el vigente.
            .OrderBy(t => t.Estatus == EstatusTicket.Anulado)
            .Select(t => new TicketViewModel
            {
                Id = t.Id,
                Numero = t.Numero,
                TipoCombustible = t.TipoCombustible.ToString(),
                Galones = t.Galones,
                Estatus = t.Estatus.ToString(),
                FechaUso = t.FechaUso,
                SolicitudTicketId = t.SolicitudTicketId,
                EntregadoA = t.SolicitudTicket!.Cargo != null
                    ? t.SolicitudTicket.Cargo.Nombre
                    : t.SolicitudTicket.Persona!.Apellido + " " + t.SolicitudTicket.Persona.Nombre
            })
            .FirstOrDefaultAsync(cancellationToken);

        return ticket is not null
            ? Result.Success(ticket)
            : Result.Failure<TicketViewModel>(TicketCombustibleErrors.NotFound(numero));
    }

    public async Task<Result<PagedData<SolicitudTicketViewModel>>> Handle(PaginationFilterQuery<SolicitudTicketViewModel> request, CancellationToken cancellationToken)
    {
        var query = uow.Query<SolicitudTicket>().Where(s => s.Status);

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            query = query.Where(s =>
                s.Proposito.Contains(request.Param) ||
                s.Tickets.Any(t => t.Numero.Contains(request.Param)));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SolicitudTicketViewModel
            {
                Id = s.Id,
                Estatus = s.Estatus.ToString(),
                TipoBeneficiario = s.TipoBeneficiario.ToString(),
                EntregadoA = s.Cargo != null
                    ? s.Cargo.Nombre
                    : s.Persona!.Apellido + " " + s.Persona.Nombre,
                Solicitante = s.SolicitantePersona!.Apellido + " " + s.SolicitantePersona.Nombre,
                DependenciaSolicitante = s.DependenciaSolicitante!.Nombre,
                Proposito = s.Proposito,
                FechaSolicitud = s.CreatedAt,
                FechaAprobacion = s.FechaAprobacion,
                MotivoRechazoCancelacion = s.MotivoRechazoCancelacion,
                TotalGalones = s.Tickets.Sum(t => t.Galones),
                Tickets = s.Tickets.OrderBy(t => t.Numero).Select(t => new TicketViewModel
                {
                    Id = t.Id,
                    Numero = t.Numero,
                    TipoCombustible = t.TipoCombustible.ToString(),
                    Galones = t.Galones,
                    Estatus = t.Estatus.ToString(),
                    FechaUso = t.FechaUso,
                    SolicitudTicketId = t.SolicitudTicketId
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<SolicitudTicketViewModel>(data, request.PageSize, request.PageNumber, totalRecords));
    }
}
