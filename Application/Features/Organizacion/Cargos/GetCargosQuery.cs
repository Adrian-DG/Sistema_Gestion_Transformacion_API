using Application.Contracts;
using Domain.Common;
using Domain.Entities.Organizacion;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Organizacion.Cargos;

public class CargoViewModel
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public Guid DependenciaId { get; set; }
    public required string Dependencia { get; set; }
    public Guid? TitularId { get; set; }
    public string? Titular { get; set; }
}

public class CargoTitularViewModel
{
    public Guid PersonaId { get; set; }
    public required string Persona { get; set; }
    public DateTime FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

public record GetCargosQuery(Guid? DependenciaId) : IRequest<Result<IReadOnlyList<CargoViewModel>>>;

public record GetHistorialTitularesQuery(Guid CargoId) : IRequest<Result<IReadOnlyList<CargoTitularViewModel>>>;

public class GetCargosQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetCargosQuery, Result<IReadOnlyList<CargoViewModel>>>,
      IRequestHandler<GetHistorialTitularesQuery, Result<IReadOnlyList<CargoTitularViewModel>>>
{
    public async Task<Result<IReadOnlyList<CargoViewModel>>> Handle(GetCargosQuery request, CancellationToken cancellationToken)
    {
        var query = uow.Query<Cargo>().Where(c => c.Status);

        if (request.DependenciaId.HasValue)
        {
            query = query.Where(c => c.DependenciaId == request.DependenciaId);
        }

        var data = await query
            .OrderBy(c => c.Dependencia!.Nombre).ThenBy(c => c.Nombre)
            .Select(c => new CargoViewModel
            {
                Id = c.Id,
                Nombre = c.Nombre,
                DependenciaId = c.DependenciaId,
                Dependencia = c.Dependencia!.Nombre,
                TitularId = c.TitularId,
                Titular = c.Titular != null ? c.Titular.Apellido + " " + c.Titular.Nombre : null
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CargoViewModel>>(data);
    }

    public async Task<Result<IReadOnlyList<CargoTitularViewModel>>> Handle(GetHistorialTitularesQuery request, CancellationToken cancellationToken)
    {
        var data = await uow.Query<CargoTitularHistorico>()
            .Where(h => h.CargoId == request.CargoId)
            .OrderByDescending(h => h.FechaDesde)
            .Select(h => new CargoTitularViewModel
            {
                PersonaId = h.PersonaId,
                Persona = h.Persona!.Identificacion + " - " + h.Persona.Apellido + " " + h.Persona.Nombre,
                FechaDesde = h.FechaDesde,
                FechaHasta = h.FechaHasta
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CargoTitularViewModel>>(data);
    }
}
