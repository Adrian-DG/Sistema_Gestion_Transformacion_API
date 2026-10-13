using API.Security;
using Application.Common.DTO;
using Application.Features.Combustible.Movimientos;
using Application.Features.Combustible.Tickets;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/combustible")]
    [AppAuthorize(AppPolicy.CanManageCombustible)]
    public class CombustibleController(IMediator mediator) : ApiControllerBase(mediator)
    {
        // Existencia y movimientos

        [HttpGet("existencia")]
        public async Task<IActionResult> GetExistencia(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetExistenciaCombustibleQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("movimientos")]
        public async Task<IActionResult> GetMovimientos([FromQuery] PaginationFilterQuery<MovimientoCombustibleViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("compras")]
        public async Task<IActionResult> RegistrarCompra([FromBody] RegistrarCompraCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("despachos")]
        public async Task<IActionResult> RegistrarDespacho([FromBody] RegistrarDespachoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        // Tickets

        [HttpGet("solicitudes-ticket")]
        public async Task<IActionResult> GetSolicitudesTicket([FromQuery] PaginationFilterQuery<SolicitudTicketViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("solicitudes-ticket")]
        public async Task<IActionResult> CreateSolicitudTicket([FromBody] CreateSolicitudTicketCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("solicitudes-ticket/{id:Guid}/aprobar")]
        public async Task<IActionResult> AprobarSolicitudTicket([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AprobarSolicitudTicketCommand(id), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("solicitudes-ticket/{id:Guid}/rechazar")]
        public async Task<IActionResult> RechazarSolicitudTicket([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RechazarSolicitudTicketCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("solicitudes-ticket/{id:Guid}/cancelar")]
        public async Task<IActionResult> CancelarSolicitudTicket([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CancelarSolicitudTicketCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("tickets/{numero}")]
        public async Task<IActionResult> GetTicket([FromRoute] string numero, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTicketByNumeroQuery(numero), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("tickets/{numero}/utilizar")]
        public async Task<IActionResult> UtilizarTicket([FromRoute] string numero, [FromBody] UtilizarTicketRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UtilizarTicketCommand(numero, request.FechaUso, request.ReceptorPersonaId, request.ReceptorNombre),
                cancellationToken);
            return HandleResult(result);
        }
    }

    public record UtilizarTicketRequest(DateTime? FechaUso, Guid? ReceptorPersonaId, string? ReceptorNombre);
}
