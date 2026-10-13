using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Domain.Common;
using Domain.Entities.Historico;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Organizacion.Personas;

public class PersonaViewModel
{
    public Guid Id { get; set; }
    public required string Identificacion { get; set; }
    public required string NombreCompleto { get; set; }
    public required string TipoPersona { get; set; }
    public string? Rango { get; set; }
    public string? Institucion { get; set; }
    public string? Dependencia { get; set; }
    public string? Telefono { get; set; }
}

public class GetPersonasQueryHandler(IUnitOfWork uow)
    : IRequestHandler<PaginationFilterQuery<PersonaViewModel>, Result<PagedData<PersonaViewModel>>>
{
    public async Task<Result<PagedData<PersonaViewModel>>> Handle(PaginationFilterQuery<PersonaViewModel> request, CancellationToken cancellationToken)
    {
        var query = uow.Query<Persona>().Where(p => p.Status);

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            query = query.Where(p =>
                p.Identificacion.Contains(request.Param) ||
                p.Nombre.Contains(request.Param) ||
                p.Apellido.Contains(request.Param));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderBy(p => p.Apellido).ThenBy(p => p.Nombre)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PersonaViewModel
            {
                Id = p.Id,
                Identificacion = p.Identificacion,
                NombreCompleto = p.Apellido + " " + p.Nombre,
                TipoPersona = p.TipoPersona.ToString(),
                Rango = p.Rango != null ? p.Rango.Nombre : null,
                Institucion = p.Institucion.HasValue ? p.Institucion.Value.ToString() : null,
                Dependencia = p.Dependencia != null ? p.Dependencia.Nombre : null,
                Telefono = p.Telefono
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<PersonaViewModel>(data, request.PageSize, request.PageNumber, totalRecords));
    }
}
