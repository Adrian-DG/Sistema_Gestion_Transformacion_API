using API.Security;
using Application.Common.DTO;
using Application.Features.Personas;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Personas que solicitan, reciben vehículos o combustible. La consulta está abierta a cualquier
    /// usuario autenticado porque se usa para seleccionar solicitantes y beneficiarios; el registro
    /// requiere permiso. Los cargos y dependencias se consultan en SIPFFAA (api/externos).
    /// </summary>
    [Authorize]
    [Route("api/personas")]
    public class PersonasController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet]
        public async Task<IActionResult> GetPersonas([FromQuery] PaginationFilterQuery<PersonaViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost]
        [AppAuthorize(AppPolicy.CanManagePersonas)]
        public async Task<IActionResult> CreatePersona([FromBody] CreatePersonaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }
}
