using Application.Features.Externos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Consultas a sistemas externos (SIPFFAA) para completar datos de personas y cargos.
    /// </summary>
    [Authorize]
    [Route("api/externos")]
    public class ExternosController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet("miembros/{cedula}")]
        public async Task<IActionResult> GetMiembro([FromRoute] string cedula, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMiembroByCedulaQuery(cedula), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("cargos")]
        public async Task<IActionResult> BuscarCargos([FromQuery] string nombre, [FromQuery] int maxResultados = 20, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new BuscarCargosExternosQuery(nombre, maxResultados), cancellationToken);
            return HandleResult(result);
        }
    }
}
