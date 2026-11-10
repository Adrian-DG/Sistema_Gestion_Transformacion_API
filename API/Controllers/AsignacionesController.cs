using Application.Features.Reportes.Asignaciones;
using API.Security;
using Application.Common.DTO;
using Application.Features.Historico.Asignaciones;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/asignaciones")]
    [Authorize]
    public class AsignacionesController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpPost]
        [AppAuthorize(AppPolicy.CanManageAsignaciones)]
        public async Task<IActionResult> Create([FromBody] CreateAsignacionCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet]
        [AppAuthorize(AppPolicy.CanVerAsignaciones)]
        public async Task<IActionResult> Get([FromQuery] FiltroAsignacionesQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Vehículos en servicio sin asignaciones aprobadas que choquen con el período.</summary>
        [HttpGet("vehiculos-disponibles")]
        [AppAuthorize(AppPolicy.CanManageAsignaciones)]
        public async Task<IActionResult> GetVehiculosDisponibles([FromQuery] DateTime salida, [FromQuery] DateTime retornoEstimado, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetVehiculosDisponiblesQuery(salida, retornoEstimado), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// PDF de la asignación con el encabezado de la plantilla de reportes y el logo de la organización
        /// (o el predeterminado). Solo las aprobadas o finalizadas son una constancia válida.
        /// </summary>
        [HttpGet("{id:Guid}/constancia")]
        [AppAuthorize(AppPolicy.CanVerAsignaciones)]
        public async Task<IActionResult> GetConstancia([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetConstanciaAsignacionQuery(id), cancellationToken);
            return result.IsSuccess
                ? File(result.Value.Contenido, "application/pdf", result.Value.NombreArchivo)
                : HandleFailure(result);
        }

        [HttpPost("{id:Guid}/aprobar")]
        [AppAuthorize(AppPolicy.CanAprobarAsignaciones)]
        public async Task<IActionResult> Aprobar([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AprobarAsignacionCommand(id), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/rechazar")]
        [AppAuthorize(AppPolicy.CanAprobarAsignaciones)]
        public async Task<IActionResult> Rechazar([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RechazarAsignacionCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/cancelar")]
        [AppAuthorize(AppPolicy.CanManageAsignaciones)]
        public async Task<IActionResult> Cancelar([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CancelarAsignacionCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/retorno")]
        [AppAuthorize(AppPolicy.CanManageAsignaciones)]
        public async Task<IActionResult> RegistrarRetorno([FromRoute] Guid id, [FromBody] RetornoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RegistrarRetornoAsignacionCommand(id, request.FechaRetorno), cancellationToken);
            return HandleResult(result);
        }
    }

    public record RetornoRequest(DateTime FechaRetorno);
}
