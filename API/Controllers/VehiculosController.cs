using API.Security;
using Application.Common.DTO;
using Application.Features.Historico.Asignaciones;
using Application.Features.Taller.Solicitudes;
using Application.Features.Vehiculo;
using Application.Security;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Flota de vehículos. No se eliminan: la baja, que se hace en el taller, los deja inactivos y
    /// conservan sus asignaciones, pólizas y despachos.
    /// </summary>
    [Route("api/vehiculos")]
    [Authorize]
    public class VehiculosController(IMediator mediator) : ApiControllerBase(mediator)
    {
        /// <summary>Registra un vehículo y devuelve su Id.</summary>
        [HttpPost]
        [AppAuthorize(AppPolicy.CanManageVehiculos)]
        public async Task<IActionResult> Create([FromBody] CreateVehiculoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Busca por placa, ficha, matrícula, chasis, marca o modelo (<c>param</c>).</summary>
        [HttpGet]
        [AppAuthorize(AppPolicy.CanVerVehiculos)]
        public async Task<IActionResult> Get([FromQuery] PaginationFilterQuery<VehiculoViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id:Guid}")]
        [AppAuthorize(AppPolicy.CanVerVehiculos)]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new IDEntityFilterQuery<VehiculoDetalleViewModel>(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Historial de asignaciones del vehículo, la de salida más reciente primero. Admite los mismos
        /// filtros que api/asignaciones (<c>estatus</c>, <c>param</c>) y solo requiere el permiso de vehículos.
        /// </summary>
        [HttpGet("{id:Guid}/asignaciones")]
        [AppAuthorize(AppPolicy.CanVerVehiculos)]
        public async Task<IActionResult> GetAsignaciones([FromRoute] Guid id, [FromQuery] FiltroAsignacionesQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query with { VehiculoId = id }, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Historial de mantenimientos: entradas al taller con sus piezas, y reactivaciones.</summary>
        [HttpGet("{id:Guid}/mantenimientos")]
        [AppAuthorize(AppPolicy.CanVerVehiculos)]
        public async Task<IActionResult> GetMantenimientos([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetHistorialMantenimientosQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Fotos vigentes del vehículo (una por vista).</summary>
        [HttpGet("{id:Guid}/fotos")]
        [AppAuthorize(AppPolicy.CanVerVehiculos)]
        public async Task<IActionResult> GetFotos([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetFotosVehiculoQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Imagen de una vista: Frente = 1, LadoIzquierdo = 2, LadoDerecho = 3, Trasera = 4.</summary>
        [HttpGet("{id:Guid}/fotos/{vista}")]
        [AppAuthorize(AppPolicy.CanVerVehiculos)]
        public async Task<IActionResult> GetFoto([FromRoute] Guid id, [FromRoute] VistaVehiculo vista, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetFotoVehiculoQuery(id, vista), cancellationToken);
            return result.IsSuccess
                ? File(result.Value.Contenido, result.Value.ContentType, result.Value.NombreArchivo)
                : HandleFailure(result);
        }

        /// <summary>Reemplaza la foto de una vista; la anterior queda en el historial.</summary>
        [HttpPut("{id:Guid}/fotos")]
        [AppAuthorize(AppPolicy.CanManageVehiculos)]
        public async Task<IActionResult> ReemplazarFoto([FromRoute] Guid id, [FromBody] FotoVehiculoRequest foto, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ReemplazarFotoVehiculoCommand(id, foto), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Actualiza los datos del vehículo; su estado lo cambia el taller.</summary>
        [HttpPut("{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageVehiculos)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] DatosVehiculoRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateVehiculoCommand(id, request.Chasis, request.Matricula, request.Placa, request.Fabricacion,
                request.MarcaId, request.ModeloId, request.TipoId, request.ColorId, request.Ficha,
                request.Kilometraje, request.TieneGarantia, request.GarantiaVence, request.GarantiaKilometraje, request.GarantiaTallerId);

            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }

    public record DatosVehiculoRequest(
        string Chasis,
        string Matricula,
        string Placa,
        int Fabricacion,
        Guid MarcaId,
        Guid ModeloId,
        Guid TipoId,
        Guid ColorId,
        string? Ficha = null,
        int? Kilometraje = null,
        bool? TieneGarantia = null,
        DateOnly? GarantiaVence = null,
        int? GarantiaKilometraje = null,
        Guid? GarantiaTallerId = null);
}
