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
        public async Task<IActionResult> GetMarcas(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMarcasQuery(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet("modelos")]
        public async Task<IActionResult> GetModelos(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetModelosQuery(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet("colores")]
        public async Task<IActionResult> GetColores(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetColoresQuery(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet("tipos-vehiculo")]
        public async Task<IActionResult> GetTiposVehiculo(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTiposVehiculoQuery(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet("tipos-documento")]
        public async Task<IActionResult> GetTipoDocumentos(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTipoDocumentosQuery(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet("tipos-operacion")]
        public async Task<IActionResult> GetTipoOperaciones(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTipoOperacionesQuery(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet("aseguradoras")]
        public async Task<IActionResult> GetAseguradoras(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAseguradorasQuery(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet("rangos")]
        public async Task<IActionResult> GetRangos(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetRangosQuery(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }
    }
}
