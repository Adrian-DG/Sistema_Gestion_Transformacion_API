using API.Security;
using Application.Common.DTO;
using Application.Features.Combustible.Movimientos;
using Application.Features.Combustible.Tickets;
using Application.Features.Reportes.Combustible;
using Application.Features.Taller.Inventario;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/combustible")]
    [Authorize]
    public class CombustibleController(IMediator mediator) : ApiControllerBase(mediator)
    {
        // Existencia y movimientos

        [HttpGet("existencia")]
        [AppAuthorize(AppPolicy.CanVerCombustible)]
        public async Task<IActionResult> GetExistencia(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetExistenciaCombustibleQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("movimientos")]
        [AppAuthorize(AppPolicy.CanVerCombustible)]
        public async Task<IActionResult> GetMovimientos([FromQuery] PaginationFilterQuery<MovimientoCombustibleViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Suplidores de combustible activos, para la entrada de combustible.</summary>
        [HttpGet("proveedores")]
        [AppAuthorize(AppPolicy.CanManageCombustible)]
        public async Task<IActionResult> GetProveedores(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetProveedoresCatalogoQuery(Domain.Enums.TipoProveedor.SuplidorCombustible), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("compras")]
        [AppAuthorize(AppPolicy.CanManageCombustible)]
        public async Task<IActionResult> RegistrarCompra([FromBody] RegistrarCompraCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("despachos")]
        [AppAuthorize(AppPolicy.CanDespacharCombustible)]
        public async Task<IActionResult> RegistrarDespacho([FromBody] RegistrarDespachoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        // Tickets

        [HttpGet("solicitudes-ticket")]
        [AppAuthorize(AppPolicy.CanVerCombustible)]
        public async Task<IActionResult> GetSolicitudesTicket([FromQuery] FiltroSolicitudesTicketQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Detalle de la solicitud: a quién se asignó, galones por tipo de combustible y cada ticket con
        /// su estado y, si se utilizó, la fecha y hora, el operador y quién recibió.
        /// </summary>
        [HttpGet("solicitudes-ticket/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanVerCombustible)]
        public async Task<IActionResult> GetSolicitudTicket([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSolicitudTicketDetalleQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Descarga un documento adjunto a una solicitud de tickets.</summary>
        [HttpGet("adjuntos/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanVerCombustible)]
        public async Task<IActionResult> GetAdjuntoSolicitudTicket([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdjuntoSolicitudTicketQuery(id), cancellationToken);
            return result.IsSuccess
                ? File(result.Value.Contenido, result.Value.ContentType, result.Value.NombreArchivo)
                : HandleFailure(result);
        }

        [HttpPost("solicitudes-ticket")]
        [AppAuthorize(AppPolicy.CanManageCombustible)]
        public async Task<IActionResult> CreateSolicitudTicket([FromBody] CreateSolicitudTicketCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("solicitudes-ticket/{id:Guid}/aprobar")]
        [AppAuthorize(AppPolicy.CanAprobarTickets)]
        public async Task<IActionResult> AprobarSolicitudTicket([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AprobarSolicitudTicketCommand(id), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("solicitudes-ticket/{id:Guid}/rechazar")]
        [AppAuthorize(AppPolicy.CanAprobarTickets)]
        public async Task<IActionResult> RechazarSolicitudTicket([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RechazarSolicitudTicketCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("solicitudes-ticket/{id:Guid}/cancelar")]
        [AppAuthorize(AppPolicy.CanManageCombustible)]
        public async Task<IActionResult> CancelarSolicitudTicket([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CancelarSolicitudTicketCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Número que recibiría el próximo ticket (referencial; se asigna al registrar la solicitud).</summary>
        [HttpGet("tickets/siguiente-numero")]
        [AppAuthorize(AppPolicy.CanManageCombustible)]
        public async Task<IActionResult> GetSiguienteNumeroTicket(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSiguienteNumeroTicketQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("tickets/{numero}")]
        [AppAuthorize(AppPolicy.CanDespacharCombustible)]
        public async Task<IActionResult> GetTicket([FromRoute] string numero, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTicketByNumeroQuery(numero), cancellationToken);
            return HandleResult(result);
        }

        // Despacho: el operador escanea el ticket, revisa sus datos y confirma.

        /// <summary>
        /// Ticket por lo leído del código de barras (número y código de verificación) o por su número.
        /// Por query string: el lector puede enviar separadores que no van en una ruta.
        /// </summary>
        [HttpGet("tickets/despacho")]
        [AppAuthorize(AppPolicy.CanDespacharCombustible)]
        public async Task<IActionResult> GetTicketDespacho([FromQuery] string codigo, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTicketDespachoQuery(codigo), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Despacha el ticket con la fecha y hora del servidor y el usuario actual como operador.</summary>
        [HttpPost("tickets/despachar")]
        [AppAuthorize(AppPolicy.CanDespacharCombustible)]
        public async Task<IActionResult> DespacharTicket([FromBody] DespacharTicketRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UtilizarTicketCommand(request.Codigo, null, request.ReceptorPersonaId, request.ReceptorNombre),
                cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Tickets despachados en el día (hoy, si no se indica la fecha).</summary>
        [HttpGet("despachos-ticket")]
        [AppAuthorize(AppPolicy.CanDespacharCombustible)]
        public async Task<IActionResult> GetDespachosTicket([FromQuery] DateTime? fecha, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetDespachosTicketQuery(fecha), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>PDF con los tickets disponibles de una solicitud aprobada, con su código de barras.</summary>
        [HttpGet("solicitudes-ticket/{id:Guid}/tickets-pdf")]
        [AppAuthorize(AppPolicy.CanManageCombustible)]
        public async Task<IActionResult> ImprimirTickets([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTicketsImpresionQuery(id), cancellationToken);
            return result.IsSuccess
                ? File(result.Value.Contenido, "application/pdf", result.Value.NombreArchivo)
                : HandleFailure(result);
        }

        /// <summary>
        /// Registra un canje con otra fecha (ej. uno que no se pudo escanear en el momento).
        /// <paramref name="numero"/> debe incluir el código de verificación si el ticket lo tiene.
        /// </summary>
        [HttpPost("tickets/{numero}/utilizar")]
        [AppAuthorize(AppPolicy.CanDespacharCombustible)]
        public async Task<IActionResult> UtilizarTicket([FromRoute] string numero, [FromBody] UtilizarTicketRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UtilizarTicketCommand(numero, request.FechaUso, request.ReceptorPersonaId, request.ReceptorNombre),
                cancellationToken);
            return HandleResult(result);
        }
    }

    public record UtilizarTicketRequest(DateTime? FechaUso, Guid? ReceptorPersonaId, string? ReceptorNombre);

    public record DespacharTicketRequest(string Codigo, Guid? ReceptorPersonaId = null, string? ReceptorNombre = null);
}
