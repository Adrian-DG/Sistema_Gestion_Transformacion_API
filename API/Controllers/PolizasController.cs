using API.Security;
using Application.Common.DTO;
using Application.Features.Historico.Polizas;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/polizas")]
    [AppAuthorize(AppPolicy.CanManagePolizas)]
    public class PolizasController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePolizaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationFilterQuery<PolizaViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }
    }
}
