using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Combustible;
using Domain.Entities.Configuracion;
using Domain.Enums;
using FluentValidation;
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
    /// <summary>Cargo o persona a quien se entregan los tickets.</summary>
    public string? EntregadoA { get; set; }
    /// <summary>Cédula de la persona, cuando se entregan a título personal.</summary>
    public string? EntregadoACedula { get; set; }
    public required string Solicitante { get; set; }
    public required string SolicitanteCedula { get; set; }
    public required string CargoSolicitante { get; set; }
    public required string DependenciaSolicitante { get; set; }
    public required string Proposito { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? MotivoRechazoCancelacion { get; set; }
    public decimal TotalGalones { get; set; }
    public List<TicketViewModel> Tickets { get; set; } = [];
}

public record GetTicketByNumeroQuery(string Numero) : IRequest<Result<TicketViewModel>>;

/// <summary>Listado de solicitudes de tickets con filtro opcional por estado.</summary>
public record FiltroSolicitudesTicketQuery : PaginationFilterQuery<SolicitudTicketViewModel>
{
    public EstatusSolicitud? Estatus { get; set; }
}

public class FiltroSolicitudesTicketQueryValidator : AbstractValidator<FiltroSolicitudesTicketQuery>
{
    public FiltroSolicitudesTicketQueryValidator()
    {
        Include(new PaginationFilterQueryValidator<SolicitudTicketViewModel>());
        RuleFor(x => x.Estatus).IsInEnum();
    }
}

/// <summary>
/// Número que recibiría el próximo ticket. Es referencial: si otro usuario registra antes, la
/// solicitud recibe los siguientes.
/// </summary>
public record GetSiguienteNumeroTicketQuery : IRequest<Result<string>>;

public class GetTicketsQueryHandler(IUnitOfWork uow) :
    IRequestHandler<GetTicketByNumeroQuery, Result<TicketViewModel>>,
    IRequestHandler<FiltroSolicitudesTicketQuery, Result<PagedData<SolicitudTicketViewModel>>>,
    IRequestHandler<GetSiguienteNumeroTicketQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetSiguienteNumeroTicketQuery request, CancellationToken cancellationToken)
    {
        var anio = DateTime.Now.Year;
        var ultimo = await uow.Query<Secuencia>()
            .Where(s => s.Codigo == TicketCombustible.CodigoSecuencia && s.Anio == anio)
            .Select(s => (long?)s.Ultimo)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        return Result.Success(TicketCombustible.FormatearNumero(anio, ultimo + 1));
    }

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
                    ? t.SolicitudTicket.Cargo
                    : t.SolicitudTicket.Persona!.Apellido + " " + t.SolicitudTicket.Persona.Nombre
            })
            .FirstOrDefaultAsync(cancellationToken);

        return ticket is not null
            ? Result.Success(ticket)
            : Result.Failure<TicketViewModel>(TicketCombustibleErrors.NotFound(numero));
    }

    public async Task<Result<PagedData<SolicitudTicketViewModel>>> Handle(FiltroSolicitudesTicketQuery request, CancellationToken cancellationToken)
    {
        var query = uow.Query<SolicitudTicket>().Where(s => s.Status);

        if (request.Estatus.HasValue)
        {
            query = query.Where(s => s.Estatus == request.Estatus);
        }

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            var texto = request.Param.Trim();
            query = query.Where(s =>
                s.Proposito.Contains(texto) ||
                s.DependenciaSolicitante.Contains(texto) ||
                s.CargoSolicitante.Contains(texto) ||
                (s.Cargo != null && s.Cargo.Contains(texto)) ||
                (s.SolicitantePersona!.Nombre + " " + s.SolicitantePersona.Apellido).Contains(texto) ||
                s.SolicitantePersona.Identificacion.Contains(texto) ||
                (s.Persona != null && ((s.Persona.Nombre + " " + s.Persona.Apellido).Contains(texto) ||
                                       s.Persona.Identificacion.Contains(texto))) ||
                s.Tickets.Any(t => t.Numero.Contains(texto)));
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
                    ? s.Cargo
                    : s.Persona!.Apellido + " " + s.Persona.Nombre,
                EntregadoACedula = s.Persona != null ? s.Persona.Identificacion : null,
                Solicitante = s.SolicitantePersona!.Apellido + " " + s.SolicitantePersona.Nombre,
                SolicitanteCedula = s.SolicitantePersona.Identificacion,
                CargoSolicitante = s.CargoSolicitante,
                DependenciaSolicitante = s.DependenciaSolicitante,
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
