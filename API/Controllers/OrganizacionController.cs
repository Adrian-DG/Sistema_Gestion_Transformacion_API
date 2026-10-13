using API.Security;
using Application.Common.DTO;
using Application.Features.Organizacion.Cargos;
using Application.Features.Organizacion.Dependencias;
using Application.Features.Organizacion.Personas;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Dependencias, cargos y personas. La consulta está abierta a cualquier usuario autenticado
    /// porque se usa para seleccionar solicitantes y beneficiarios; el registro requiere permiso.
    /// </summary>
    [Authorize]
    [Route("api/organizacion")]
    public class OrganizacionController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet("dependencias")]
        public async Task<IActionResult> GetDependencias(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetDependenciasQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("dependencias")]
        [AppAuthorize(AppPolicy.CanManageOrganizacion)]
        public async Task<IActionResult> CreateDependencia([FromBody] CreateDependenciaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("cargos")]
        public async Task<IActionResult> GetCargos([FromQuery] Guid? dependenciaId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCargosQuery(dependenciaId), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("cargos")]
        [AppAuthorize(AppPolicy.CanManageOrganizacion)]
        public async Task<IActionResult> CreateCargo([FromBody] CreateCargoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("cargos/{id:Guid}/titulares")]
        public async Task<IActionResult> GetHistorialTitulares([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetHistorialTitularesQuery(id), cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("cargos/{id:Guid}/titular")]
        [AppAuthorize(AppPolicy.CanManageOrganizacion)]
        public async Task<IActionResult> CambiarTitular([FromRoute] Guid id, [FromBody] CambiarTitularRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CambiarTitularCargoCommand(id, request.PersonaId, request.FechaCambio), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("personas")]
        public async Task<IActionResult> GetPersonas([FromQuery] PaginationFilterQuery<PersonaViewModel> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("personas")]
        [AppAuthorize(AppPolicy.CanManageOrganizacion)]
        public async Task<IActionResult> CreatePersona([FromBody] CreatePersonaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }

    public record CambiarTitularRequest(Guid? PersonaId, DateTime? FechaCambio);
}
