using API.Security;
using Application.Common.DTO;
using Application.Features.Taller;
using Application.Features.Taller.Solicitudes;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Taller: solicitudes (registro, ingreso con mecánico, piezas utilizadas, cierre con dictamen),
    /// mecánicos, recibos y facturas adjuntos, y la reactivación excepcional de vehículos dados de baja.
    /// </summary>
    [Authorize]
    [Route("api/taller")]
    public class TallerController(IMediator mediator) : ApiControllerBase(mediator)
    {
        // Solicitudes

        [HttpGet("solicitudes")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> GetSolicitudes([FromQuery] FiltroSolicitudesTallerQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Solicitud con sus piezas utilizadas, compras directas (con recibo) y devoluciones.</summary>
        [HttpGet("solicitudes/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> GetSolicitud([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSolicitudTallerQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Registra la solicitud (Pendiente) y devuelve su Id y número.</summary>
        [HttpPost("solicitudes")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> CreateSolicitud([FromBody] CreateSolicitudTallerCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Ingreso del vehículo con el mecánico asignado; pasa a En mantenimiento. Devuelve cuántas
        /// asignaciones aprobadas a futuro tiene el vehículo, que habrá que reprogramar.
        /// </summary>
        [HttpPost("solicitudes/{id:Guid}/ingresar")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> Ingresar([FromRoute] Guid id, [FromBody] IngresoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new IngresarSolicitudTallerCommand(id, request.MecanicoId, request.Kilometraje), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Registra piezas utilizadas. Se toma lo disponible en inventario y lo faltante se compra
        /// (<c>compra</c> con costo, proveedor y recibo); sin compra responde 409 Taller.CompraRequerida.
        /// </summary>
        [HttpPost("solicitudes/{id:Guid}/piezas")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> AgregarPieza([FromRoute] Guid id, [FromBody] PiezaRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AgregarPiezaSolicitudTallerCommand(id, request.PiezaId, request.Cantidad, request.Compra), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Cierra la orden; con <c>procedeBaja</c> el vehículo pasa a inactivo, si no vuelve a operativo.</summary>
        [HttpPost("solicitudes/{id:Guid}/finalizar")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> Finalizar([FromRoute] Guid id, [FromBody] FinalizarRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new FinalizarSolicitudTallerCommand(id, request.Diagnostico, request.TrabajoRealizado, request.ProcedeBaja), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("solicitudes/{id:Guid}/rechazar")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> Rechazar([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RechazarSolicitudTallerCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Anula la solicitud; si el vehículo estaba en el taller sale y las piezas vuelven al inventario.</summary>
        [HttpPost("solicitudes/{id:Guid}/cancelar")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> Cancelar([FromRoute] Guid id, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CancelarSolicitudTallerCommand(id, request.Motivo), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Descarga una factura de entrada o un recibo de compra directa.</summary>
        [HttpGet("adjuntos/{id:Guid}")]
        [AppAuthorize(AppPermission.GestionTaller, AppPermission.GestionInventarioPiezas, AppPermission.Administrador)]
        public async Task<IActionResult> GetAdjunto([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdjuntoTallerQuery(id), cancellationToken);
            return result.IsSuccess
                ? File(result.Value.Contenido, result.Value.ContentType, result.Value.NombreArchivo)
                : HandleFailure(result);
        }

        // Mecánicos

        [HttpGet("mecanicos")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> GetMecanicos([FromQuery] FiltroMecanicosQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Mecánicos activos, para asignarlos al ingreso.</summary>
        [HttpGet("mecanicos/catalogo")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> GetMecanicosCatalogo(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMecanicosCatalogoQuery(), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Militar: <c>personaId</c> importado de SIPFFAA. Civil: <c>civil</c> con sus datos.</summary>
        [HttpPost("mecanicos")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> CreateMecanico([FromBody] CreateMecanicoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("mecanicos/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageTaller)]
        public async Task<IActionResult> UpdateMecanico([FromRoute] Guid id, [FromBody] DatosMecanicoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new UpdateMecanicoCommand(id, request.Especialidad, request.Telefono, request.Activo), cancellationToken);
            return HandleResult(result);
        }

        // Reactivación

        /// <summary>Reactiva por excepción un vehículo inactivo; queda en su historial de mantenimientos.</summary>
        [HttpPost("vehiculos/{vehiculoId:Guid}/reactivar")]
        [AppAuthorize(AppPolicy.CanReactivarVehiculos)]
        public async Task<IActionResult> ReactivarVehiculo([FromRoute] Guid vehiculoId, [FromBody] MotivoRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ReactivarVehiculoCommand(vehiculoId, request.Motivo), cancellationToken);
            return HandleResult(result);
        }
    }

    public record IngresoRequest(Guid MecanicoId, int? Kilometraje);

    public record PiezaRequest(Guid PiezaId, decimal Cantidad, CompraPiezaRequest? Compra);

    public record FinalizarRequest(string Diagnostico, string TrabajoRealizado, bool ProcedeBaja);

    public record DatosMecanicoRequest(string? Especialidad, string? Telefono, bool Activo);
}
