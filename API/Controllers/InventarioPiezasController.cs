using API.Security;
using Application.Features.Taller.Inventario;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Inventario del taller: piezas con su existencia y kardex, proveedores y entradas (compras al
    /// inventario). El personal del taller también puede consultar las piezas y proveedores activos y
    /// registrar piezas nuevas, porque los necesita al registrar las piezas de una orden.
    /// </summary>
    [Authorize]
    [Route("api/inventario-piezas")]
    public class InventarioPiezasController(IMediator mediator) : ApiControllerBase(mediator)
    {
        // Piezas

        [HttpGet("piezas")]
        [AppAuthorize(AppPermission.GestionInventarioPiezas, AppPermission.GestionTaller, AppPermission.Administrador)]
        public async Task<IActionResult> GetPiezas([FromQuery] FiltroPiezasQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Piezas activas con su existencia, para listas de selección.</summary>
        [HttpGet("piezas/catalogo")]
        [AppAuthorize(AppPermission.GestionInventarioPiezas, AppPermission.GestionTaller, AppPermission.Administrador)]
        public async Task<IActionResult> GetPiezasCatalogo(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPiezasCatalogoQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("piezas")]
        [AppAuthorize(AppPermission.GestionInventarioPiezas, AppPermission.GestionTaller, AppPermission.Administrador)]
        public async Task<IActionResult> CreatePieza([FromBody] CreatePiezaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("piezas/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> GetPieza([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPiezaQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Actualiza la pieza; <c>activo: false</c> la desactiva.</summary>
        [HttpPut("piezas/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> UpdatePieza([FromRoute] Guid id, [FromBody] DatosPiezaRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdatePiezaCommand(id, request.Codigo, request.Nombre, request.Descripcion,
                request.UnidadMedida, request.ExistenciaMinima, request.Activo, request.CodigoBarras);
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Kardex de la pieza, el movimiento más reciente primero.</summary>
        [HttpGet("piezas/{id:Guid}/movimientos")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> GetMovimientos([FromRoute] Guid id, [FromQuery] FiltroMovimientosPiezaQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query with { PiezaId = id }, cancellationToken);
            return HandleResult(result);
        }

        // Proveedores

        [HttpGet("proveedores")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> GetProveedores([FromQuery] FiltroProveedoresQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Proveedores activos, para listas de selección.</summary>
        [HttpGet("proveedores/catalogo")]
        [AppAuthorize(AppPermission.GestionInventarioPiezas, AppPermission.GestionTaller, AppPermission.Administrador)]
        public async Task<IActionResult> GetProveedoresCatalogo(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetProveedoresCatalogoQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("proveedores")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> CreateProveedor([FromBody] CreateProveedorCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("proveedores/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> GetProveedor([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetProveedorQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Actualiza el proveedor; <c>activo: false</c> lo desactiva.</summary>
        [HttpPut("proveedores/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> UpdateProveedor([FromRoute] Guid id, [FromBody] DatosProveedorRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateProveedorCommand(id, request.Nombre, request.Rnc, request.Telefono, request.Correo,
                request.Direccion, request.Contacto, request.Activo);
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        // Entradas al inventario

        [HttpGet("entradas")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> GetEntradas([FromQuery] FiltroEntradasPiezaQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("entradas")]
        [AppAuthorize(AppPolicy.CanManageInventarioPiezas)]
        public async Task<IActionResult> RegistrarEntrada([FromBody] RegistrarEntradaPiezaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }

    public record DatosPiezaRequest(string Codigo, string Nombre, string? Descripcion, string UnidadMedida, decimal ExistenciaMinima, bool Activo, string? CodigoBarras = null);

    public record DatosProveedorRequest(string Nombre, string? Rnc, string? Telefono, string? Correo, string? Direccion, string? Contacto, bool Activo);
}
