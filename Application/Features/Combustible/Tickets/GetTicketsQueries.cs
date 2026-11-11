using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Application.Contracts.Usuarios;
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

/// <summary>Ticket leído en el despacho: lo necesario para que el operador confirme el canje.</summary>
public class TicketDespachoViewModel
{
    public Guid Id { get; set; }
    public required string Numero { get; set; }
    public required string TipoCombustible { get; set; }
    public decimal Galones { get; set; }
    public required string Estatus { get; set; }
    public string? EntregadoA { get; set; }
    public string? Proposito { get; set; }
    /// <summary>
    /// El ticket tiene código de verificación y no se leyó (se escribió solo el número): hay que escanearlo
    /// o escribir también el código.
    /// </summary>
    public bool RequiereCodigoVerificacion { get; set; }
    public DateTime? FechaUso { get; set; }
    public string? DespachadoPor { get; set; }
    public string? ReceptorNombre { get; set; }
}

/// <summary>Ticket despachado (canjeado): cuándo, quién lo despachó y quién recibió el combustible.</summary>
public class DespachoTicketViewModel
{
    public Guid MovimientoId { get; set; }
    public required string Numero { get; set; }
    public required string TipoCombustible { get; set; }
    public decimal Galones { get; set; }
    public DateTime FechaUso { get; set; }
    public string? EntregadoA { get; set; }
    public string? ReceptorNombre { get; set; }
    public string? DespachadoPor { get; set; }
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

/// <summary>
/// Ticket por lo leído del código de barras (número y código de verificación) o por su número escrito.
/// Si se leyó un código de verificación que no coincide, responde como no válido sin mostrar el ticket.
/// </summary>
public record GetTicketDespachoQuery(string Codigo) : IRequest<Result<TicketDespachoViewModel>>;

/// <summary>Tickets despachados en el día indicado (hoy, si se omite), el más reciente primero.</summary>
public record GetDespachosTicketQuery(DateTime? Fecha = null) : IRequest<Result<IReadOnlyList<DespachoTicketViewModel>>>;

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

public class GetTicketsQueryHandler(IUnitOfWork uow, IUsuarioService usuarios) :
    IRequestHandler<GetTicketByNumeroQuery, Result<TicketViewModel>>,
    IRequestHandler<GetTicketDespachoQuery, Result<TicketDespachoViewModel>>,
    IRequestHandler<GetDespachosTicketQuery, Result<IReadOnlyList<DespachoTicketViewModel>>>,
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

    public async Task<Result<TicketDespachoViewModel>> Handle(GetTicketDespachoQuery request, CancellationToken cancellationToken)
    {
        var (numero, codigo) = TicketCombustible.InterpretarCodigo(request.Codigo);
        var ticket = await uow.Query<TicketCombustible>()
            .Where(t => t.Numero == numero)
            // Si el número fue anulado y registrado de nuevo, se muestra el vigente.
            .OrderBy(t => t.Estatus == EstatusTicket.Anulado)
            .Select(t => new
            {
                t.Id,
                t.Numero,
                t.TipoCombustible,
                t.Galones,
                t.Estatus,
                t.CodigoVerificacion,
                t.FechaUso,
                t.DespachadoPor,
                EntregadoA = t.SolicitudTicket!.Cargo != null
                    ? t.SolicitudTicket.Cargo
                    : t.SolicitudTicket.Persona!.Apellido + " " + t.SolicitudTicket.Persona.Nombre,
                t.SolicitudTicket.Proposito
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ticket is null) return Result.Failure<TicketDespachoViewModel>(TicketCombustibleErrors.NotFound(numero));

        // Un código leído que no coincide indica un ticket falso o alterado: no se muestran sus datos.
        if (ticket.CodigoVerificacion is not null && codigo is not null &&
            !string.Equals(ticket.CodigoVerificacion, codigo, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<TicketDespachoViewModel>(TicketCombustibleErrors.CodigoInvalido(numero));
        }

        string? despachadoPor = null;
        string? receptor = null;
        if (ticket.Estatus == EstatusTicket.Utilizado)
        {
            var canje = await uow.Query<MovimientoCombustible>()
                .Where(m => m.TicketCombustibleId == ticket.Id && m.Status)
                .Select(m => new
                {
                    m.CreatedBy,
                    Receptor = m.ReceptorNombre ?? (m.ReceptorPersona != null
                        ? m.ReceptorPersona.Apellido + " " + m.ReceptorPersona.Nombre
                        : null)
                })
                .FirstOrDefaultAsync(cancellationToken);
            receptor = canje?.Receptor;

            // Los canjes anteriores a este módulo no guardaban el operador en el ticket.
            if ((ticket.DespachadoPor ?? canje?.CreatedBy) is { } operador)
            {
                despachadoPor = (await usuarios.GetNombresAsync([operador], cancellationToken)).GetValueOrDefault(operador);
            }
        }

        return Result.Success(new TicketDespachoViewModel
        {
            Id = ticket.Id,
            Numero = ticket.Numero,
            TipoCombustible = ticket.TipoCombustible.ToString(),
            Galones = ticket.Galones,
            Estatus = ticket.Estatus.ToString(),
            EntregadoA = ticket.EntregadoA,
            Proposito = ticket.Proposito,
            RequiereCodigoVerificacion = ticket.CodigoVerificacion is not null && codigo is null,
            FechaUso = ticket.FechaUso,
            DespachadoPor = despachadoPor,
            ReceptorNombre = receptor
        });
    }

    public async Task<Result<IReadOnlyList<DespachoTicketViewModel>>> Handle(GetDespachosTicketQuery request, CancellationToken cancellationToken)
    {
        var desde = (request.Fecha ?? DateTime.Now).Date;
        var hasta = desde.AddDays(1);

        var despachos = await uow.Query<MovimientoCombustible>()
            .Where(m => m.Status && m.Concepto == ConceptoMovimientoCombustible.Ticket &&
                        m.Fecha >= desde && m.Fecha < hasta && m.TicketCombustibleId != null)
            .OrderByDescending(m => m.Fecha)
            .Select(m => new
            {
                m.Id,
                m.TicketCombustible!.Numero,
                m.TipoCombustible,
                m.Galones,
                m.Fecha,
                EntregadoA = m.TicketCombustible.SolicitudTicket!.Cargo != null
                    ? m.TicketCombustible.SolicitudTicket.Cargo
                    : m.TicketCombustible.SolicitudTicket.Persona!.Apellido + " " + m.TicketCombustible.SolicitudTicket.Persona.Nombre,
                ReceptorNombre = m.ReceptorNombre ?? (m.ReceptorPersona != null
                    ? m.ReceptorPersona.Apellido + " " + m.ReceptorPersona.Nombre
                    : null),
                // Los canjes anteriores a este módulo no guardaban el operador en el ticket.
                Operador = m.TicketCombustible.DespachadoPor ?? m.CreatedBy
            })
            .ToListAsync(cancellationToken);

        var nombres = await usuarios.GetNombresAsync(despachos.Select(d => d.Operador).Distinct().ToList(), cancellationToken);

        IReadOnlyList<DespachoTicketViewModel> data = despachos.Select(d => new DespachoTicketViewModel
        {
            MovimientoId = d.Id,
            Numero = d.Numero,
            TipoCombustible = d.TipoCombustible.ToString(),
            Galones = d.Galones,
            FechaUso = d.Fecha,
            EntregadoA = d.EntregadoA,
            ReceptorNombre = d.ReceptorNombre,
            DespachadoPor = nombres.GetValueOrDefault(d.Operador)
        }).ToList();

        return Result.Success(data);
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
