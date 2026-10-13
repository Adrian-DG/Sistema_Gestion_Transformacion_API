using API.Security;
using Application.Features.Usuarios;
using Application.Security;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/usuarios")]
    [AppAuthorize(AppPolicy.CanManageUsuarios)]
    public class UsuariosController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetUsuariosQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUsuarioByIdQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Permisos disponibles para asignar.</summary>
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetRolesQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUsuarioCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUsuarioRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateUsuarioCommand(id, request.UserName, request.Email, request.Telefono, request.Identificacion,
                request.Nombre, request.Apellido, request.Sexo, request.Institucion, request.RangoId);

            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("{id:Guid}/roles")]
        public async Task<IActionResult> AsignarRoles([FromRoute] Guid id, [FromBody] AsignarRolesRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AsignarRolesUsuarioCommand(id, request.Roles), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/activar")]
        public async Task<IActionResult> Activar([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CambiarEstadoUsuarioCommand(id, true), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/desactivar")]
        public async Task<IActionResult> Desactivar([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CambiarEstadoUsuarioCommand(id, false), cancellationToken);
            return HandleResult(result);
        }
    }

    public record UpdateUsuarioRequest(
        string UserName,
        string Email,
        string? Telefono,
        string Identificacion,
        string Nombre,
        string Apellido,
        SexoEnum Sexo,
        InstitucionEnum Institucion,
        Guid? RangoId);

    public record AsignarRolesRequest(List<string> Roles);
}
