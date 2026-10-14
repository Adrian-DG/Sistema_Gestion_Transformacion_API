using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using Domain.Entities.Combustible;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Movimientos;

public class MovimientoCombustibleViewModel
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; }
    public required string Concepto { get; set; }
    public bool EsEntrada { get; set; }
    public required string TipoCombustible { get; set; }
    public decimal Galones { get; set; }
    public string? Proposito { get; set; }
    public string? Proveedor { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Solicitante { get; set; }
    public string? DependenciaSolicitante { get; set; }
    public Guid? AsignacionId { get; set; }
    public string? Vehiculo { get; set; }
    public string? DescripcionConjunto { get; set; }
    public int? CantidadUnidades { get; set; }
    public string? NumeroTicket { get; set; }
    public string? Receptor { get; set; }
}

public record GetExistenciaCombustibleQuery : IRequest<Result<IReadOnlyList<ExistenciaCombustibleViewModel>>>;

public class GetMovimientosQueryHandler(IUnitOfWork uow) :
    IRequestHandler<GetExistenciaCombustibleQuery, Result<IReadOnlyList<ExistenciaCombustibleViewModel>>>,
    IRequestHandler<PaginationFilterQuery<MovimientoCombustibleViewModel>, Result<PagedData<MovimientoCombustibleViewModel>>>
{
    public async Task<Result<IReadOnlyList<ExistenciaCombustibleViewModel>>> Handle(GetExistenciaCombustibleQuery request, CancellationToken cancellationToken)
    {
        var data = new List<ExistenciaCombustibleViewModel>();

        foreach (var tipo in Enum.GetValues<TipoCombustible>())
        {
            data.Add(await uow.ObtenerExistenciaAsync(tipo, cancellationToken));
        }

        return Result.Success<IReadOnlyList<ExistenciaCombustibleViewModel>>(data);
    }

    public async Task<Result<PagedData<MovimientoCombustibleViewModel>>> Handle(PaginationFilterQuery<MovimientoCombustibleViewModel> request, CancellationToken cancellationToken)
    {
        var query = uow.Query<MovimientoCombustible>().Where(m => m.Status);

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            query = query.Where(m =>
                (m.Proposito != null && m.Proposito.Contains(request.Param)) ||
                (m.Proveedor != null && m.Proveedor.Contains(request.Param)) ||
                (m.NumeroFactura != null && m.NumeroFactura.Contains(request.Param)) ||
                (m.DescripcionConjunto != null && m.DescripcionConjunto.Contains(request.Param)) ||
                (m.TicketCombustible != null && m.TicketCombustible.Numero.Contains(request.Param)) ||
                (m.Asignacion != null && m.Asignacion.Vehiculo!.Placa.Contains(request.Param)));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(m => m.Fecha)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MovimientoCombustibleViewModel
            {
                Id = m.Id,
                Fecha = m.Fecha,
                Concepto = m.Concepto.ToString(),
                EsEntrada = m.Concepto == ConceptoMovimientoCombustible.Compra,
                TipoCombustible = m.TipoCombustible.ToString(),
                Galones = m.Galones,
                Proposito = m.Proposito,
                Proveedor = m.Proveedor,
                NumeroFactura = m.NumeroFactura,
                Solicitante = m.SolicitantePersona != null ? m.SolicitantePersona.Apellido + " " + m.SolicitantePersona.Nombre : null,
                DependenciaSolicitante = m.DependenciaSolicitante,
                AsignacionId = m.AsignacionId,
                Vehiculo = m.Asignacion != null
                    ? m.Asignacion.Vehiculo!.Placa + " | " + m.Asignacion.Vehiculo.Marca!.Nombre + " | " + m.Asignacion.Vehiculo.Modelo!.Nombre
                    : null,
                DescripcionConjunto = m.DescripcionConjunto,
                CantidadUnidades = m.CantidadUnidades,
                NumeroTicket = m.TicketCombustible != null ? m.TicketCombustible.Numero : null,
                Receptor = m.ReceptorPersona != null
                    ? m.ReceptorPersona.Apellido + " " + m.ReceptorPersona.Nombre
                    : m.ReceptorNombre
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<MovimientoCombustibleViewModel>(data, request.PageSize, request.PageNumber, totalRecords));
    }
}
