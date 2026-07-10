using Application.Features.Misc;
using Application.Features.Misc.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/catalogos")]
    public class CatalogosController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet("marcas")]
        public async Task<IReadOnlyList<CatalogItemViewModel>> GetMarcas(CancellationToken cancellationToken)
            => await _mediator.Send(new GetMarcasQuery(), cancellationToken);

        [HttpGet("modelos")]
        public async Task<IReadOnlyList<ModeloViewModel>> GetModelos(CancellationToken cancellationToken)
            => await _mediator.Send(new GetModelosQuery(), cancellationToken);

        [HttpGet("colores")]
        public async Task<IReadOnlyList<CatalogItemViewModel>> GetColores(CancellationToken cancellationToken)
            => await _mediator.Send(new GetColoresQuery(), cancellationToken);

        [HttpGet("tipos-vehiculo")]
        public async Task<IReadOnlyList<CatalogItemViewModel>> GetTiposVehiculo(CancellationToken cancellationToken)
            => await _mediator.Send(new GetTiposVehiculoQuery(), cancellationToken);

        [HttpGet("tipos-documento")]
        public async Task<IReadOnlyList<CatalogItemViewModel>> GetTipoDocumentos(CancellationToken cancellationToken)
            => await _mediator.Send(new GetTipoDocumentosQuery(), cancellationToken);

        [HttpGet("tipos-operacion")]
        public async Task<IReadOnlyList<CatalogItemViewModel>> GetTipoOperaciones(CancellationToken cancellationToken)
            => await _mediator.Send(new GetTipoOperacionesQuery(), cancellationToken);

        [HttpGet("aseguradoras")]
        public async Task<IReadOnlyList<CatalogItemViewModel>> GetAseguradoras(CancellationToken cancellationToken)
            => await _mediator.Send(new GetAseguradorasQuery(), cancellationToken);

        [HttpGet("rangos")]
        public async Task<IReadOnlyList<RangoViewModel>> GetRangos(CancellationToken cancellationToken)
            => await _mediator.Send(new GetRangosQuery(), cancellationToken);
    }
}
