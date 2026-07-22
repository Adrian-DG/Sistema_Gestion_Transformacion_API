using API.Security;
using Application.Common.DTO;
using Application.Common.Response;
using Application.Features.Vehiculo;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/vehiculos")]
    [AppAuthorize(AppPolicy.CanManageVehiculos)]
    public class VehiculosController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehiculoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationFilterQuery<VehiculoViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new IDEntityFilterQuery<VehiculoViewModel>(id), cancellationToken);
            return HandleResult(result);
        }
    }
}
