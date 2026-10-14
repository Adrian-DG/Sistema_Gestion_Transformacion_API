using API.Security;
using Application.Common.DTO;
using Application.Features.Historico.Asignaciones;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/asignaciones")]
    [AppAuthorize(AppPolicy.CanManageAsignaciones)]
    public class AsignacionesController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAsignacionCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] FiltroAsignacionesQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Vehículos en servicio sin asignaciones aprobadas que choquen con el período.</summary>
        [HttpGet("vehiculos-disponibles")]
        public async Task<IActionResult> GetVehiculosDisponibles([FromQuery] DateTime salida, [FromQuery] DateTime retornoEstimado, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetVehiculosDisponiblesQuery(salida, retornoEstimado), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/aprobar")]
        public async Task<IActionResult> Aprobar([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AprobarAsignacionCommand(id), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/rechazar")]
        public async Task<IActionResult> Rechazar([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RechazarAsignacionCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/cancelar")]
        public async Task<IActionResult> Cancelar([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CancelarAsignacionCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/retorno")]
        public async Task<IActionResult> RegistrarRetorno([FromRoute] Guid id, [FromBody] RetornoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RegistrarRetornoAsignacionCommand(id, request.FechaRetorno), cancellationToken);
            return HandleResult(result);
        }
    }

    public record RetornoRequest(DateTime FechaRetorno);
}
