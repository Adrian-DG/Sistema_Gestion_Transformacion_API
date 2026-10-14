using API.Security;
using Application.Common.DTO;
using Application.Features.Vehiculo;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Flota de vehículos. No se eliminan: un vehículo fuera de servicio se marca como no disponible
    /// y conserva sus asignaciones, pólizas y despachos.
    /// </summary>
    [Route("api/vehiculos")]
    [AppAuthorize(AppPolicy.CanManageVehiculos)]
    public class VehiculosController(IMediator mediator) : ApiControllerBase(mediator)
    {
        /// <summary>Registra un vehículo y devuelve su Id.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehiculoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Busca por placa, matrícula, chasis, marca o modelo (<c>param</c>).</summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationFilterQuery<VehiculoViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new IDEntityFilterQuery<VehiculoDetalleViewModel>(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Actualiza el vehículo; <c>disponible: false</c> lo deja fuera de servicio.</summary>
        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] DatosVehiculoRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateVehiculoCommand(id, request.Chasis, request.Matricula, request.Placa, request.Fabricacion,
                request.MarcaId, request.ModeloId, request.TipoId, request.ColorId, request.Disponible);

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
        bool Disponible);
}
