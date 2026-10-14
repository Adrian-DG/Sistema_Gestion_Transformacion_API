using API.Security;
using Application.Common.DTO;
using Application.Features.Historico.Polizas;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Pólizas de seguro de los vehículos. No se eliminan: al renovar se registra una póliza nueva y la
    /// anterior queda en el historial.
    /// </summary>
    [Route("api/polizas")]
    [AppAuthorize(AppPolicy.CanManagePolizas)]
    public class PolizasController(IMediator mediator) : ApiControllerBase(mediator)
    {
        /// <summary>Registra una póliza y devuelve su Id.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePolizaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Busca por número, placa, matrícula o aseguradora (<c>param</c>). Cada póliza indica su cobertura
        /// (Vigente, PorVencer o Vencida) y los días que le quedan.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationFilterQuery<PolizaViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new IDEntityFilterQuery<PolizaDetalleViewModel>(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Corrige los datos de la póliza; para renovarla registre una nueva.</summary>
        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] DatosPolizaRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdatePolizaCommand(id, request.VehiculoId, request.AseguradoraId, request.Numero,
                request.FechaExpedicion, request.FechaEfectividad, request.FechaVencimiento);

            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }

    public record DatosPolizaRequest(
        Guid VehiculoId,
        Guid AseguradoraId,
        string Numero,
        DateOnly FechaExpedicion,
        DateOnly FechaEfectividad,
        DateOnly FechaVencimiento);
}
