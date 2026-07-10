using API.Security;
using Application.Common.DTO;
using Application.Features.Historico.Asignaciones;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/asignaciones")]
    [AppAuthorize(AppPolicy.CanManageAsignaciones)]
    public class AsignacionesController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAsignacionCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationFilterQuery<AsignacionViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }
    }
}
