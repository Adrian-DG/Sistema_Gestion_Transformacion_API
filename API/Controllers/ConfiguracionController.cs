using API.Security;
using Application.Features.Configuracion;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Personalización del frontend. La lectura es pública porque la pantalla de inicio de sesión
    /// necesita el logo, el nombre y los colores antes de que exista un token.
    /// </summary>
    [Route("api/configuracion")]
    public class ConfiguracionController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet("apariencia")]
        [AllowAnonymous]
        public async Task<IActionResult> GetApariencia(CancellationToken cancellationToken)
        {
            Response.Headers.CacheControl = "no-cache";
            var result = await _mediator.Send(new GetAparienciaQuery(), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Imagen del logo. El frontend debe pedirla como <c>logo?v={version}</c>: así el navegador puede
        /// guardarla en caché y se descarga de nuevo solo cuando cambia la versión.
        /// </summary>
        [HttpGet("apariencia/logo")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLogo(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetLogoQuery(), cancellationToken);
            if (result.IsFailure) return HandleFailure(result);

            Response.Headers.CacheControl = "public, max-age=86400";
            Response.Headers.ETag = $"\"logo-{result.Value.Version}\"";
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(result.Value.Contenido, result.Value.ContentType);
        }

        [HttpPut("apariencia")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> UpdateApariencia([FromBody] UpdateAparienciaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("apariencia/logo")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> ActualizarLogo([FromBody] ActualizarLogoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpDelete("apariencia/logo")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> EliminarLogo(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new EliminarLogoCommand(), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("apariencia/restablecer")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> Restablecer(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RestablecerAparienciaCommand(), cancellationToken);
            return HandleResult(result);
        }
    }
}
