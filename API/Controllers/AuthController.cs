using API.Security;
using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Features.Authentication;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [AllowAnonymous]
    [Route("api/authentication")]
    public class AuthController(IMediator mediator) : ApiControllerBase(mediator)
    {
        /// <summary>Inicia sesión.</summary>
        /// <remarks>
        /// Devuelve el token JWT que se envía en el encabezado <c>Authorization: Bearer {token}</c> de las
        /// demás solicitudes. El token incluye los permisos del usuario.
        /// </remarks>
        /// <response code="200">Token JWT.</response>
        /// <response code="400">Usuario o contraseña con formato inválido.</response>
        /// <response code="401">Usuario o contraseña incorrectos (no se indica cuál de los dos).</response>
        /// <response code="403">El usuario está desactivado.</response>
        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]        public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }
    }
}
