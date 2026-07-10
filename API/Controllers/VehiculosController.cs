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
            await _mediator.Send(command, cancellationToken);
            return Ok();
        }

        [HttpGet]
        public async Task<PagedData<VehiculoViewModel>> Get([FromQuery] PaginationFilterQuery<VehiculoViewModel> query, CancellationToken cancellationToken)
        {
            return await _mediator.Send(query, cancellationToken);
        }
    }
}
