using Application.Contracts;
using Application.Features.Misc.ViewModels;
using Domain.Common;
using Domain.Entities.Misc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Misc;

/// <summary>
/// Tipos de documento activos. Con <paramref name="TipoOperacionId"/> (ver TiposOperacion), solo los
/// permitidos en ese flujo.
/// </summary>
public record GetTipoDocumentosQuery(Guid? TipoOperacionId = null) : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetTipoDocumentosQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetTipoDocumentosQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetTipoDocumentosQuery request, CancellationToken cancellationToken)
    {
        var query = uow.Query<TipoDocumento>().Where(t => t.Status);

        if (request.TipoOperacionId is { } operacion)
        {
            var permitidos = uow.Query<TipoDocumentoOperacion>()
                .Where(p => p.TipoOperacionId == operacion)
                .Select(p => p.TipoDocumentoId);
            query = query.Where(t => permitidos.Contains(t.Id));
        }

        var data = await query
            .OrderBy(t => t.Nombre)
            .Select(t => new CatalogItemViewModel { Id = t.Id, Nombre = t.Nombre })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CatalogItemViewModel>>(data);
    }
}
