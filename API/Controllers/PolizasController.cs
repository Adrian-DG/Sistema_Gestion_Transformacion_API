using API.Security;
using Application.Common.DTO;
using Application.Common.Response;
using Application.Features.Historico;
using Application.Features.Historico.ViewModels;
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
        public async Task<ActionResult<Guid>> Create([FromBody] CreatePolizaCommand command, CancellationToken cancellationToken)
        {
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(id);
        }

        [HttpGet]
        public async Task<PagedData<PolizaViewModel>> Get([FromQuery] PaginationFilterQuery<PolizaViewModel> query, CancellationToken cancellationToken)
        {
            return await _mediator.Send(query, cancellationToken);
        }
    }
}
