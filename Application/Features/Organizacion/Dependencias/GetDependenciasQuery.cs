using Application.Contracts;
using Domain.Common;
using Domain.Entities.Organizacion;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Organizacion.Dependencias;

public class DependenciaViewModel
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public string? Siglas { get; set; }
    public required string Institucion { get; set; }
}

public record GetDependenciasQuery : IRequest<Result<IReadOnlyList<DependenciaViewModel>>>;

public class GetDependenciasQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetDependenciasQuery, Result<IReadOnlyList<DependenciaViewModel>>>
{
    public async Task<Result<IReadOnlyList<DependenciaViewModel>>> Handle(GetDependenciasQuery request, CancellationToken cancellationToken)
    {
        var data = await uow.Query<Dependencia>()
            .Where(d => d.Status)
            .OrderBy(d => d.Nombre)
            .Select(d => new DependenciaViewModel
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Siglas = d.Siglas,
                Institucion = d.Institucion.ToString()
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DependenciaViewModel>>(data);
    }
}
