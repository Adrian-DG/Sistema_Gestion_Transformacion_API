using API.Security;
using Application.Features.Configuracion;
using Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Personalización del frontend. La lectura de la apariencia activa es pública porque la pantalla de
    /// inicio de sesión necesita el logo, el nombre y los colores antes de que exista un token. Las
    /// apariencias guardadas solo las administra el Administrador y no se eliminan: se activa otra.
    /// </summary>
    [Route("api/configuracion")]
    public class ConfiguracionController(IMediator mediator) : ApiControllerBase(mediator)
    {
        /// <summary>Apariencia activa.</summary>
        [HttpGet("apariencia")]
        [AllowAnonymous]
        public async Task<IActionResult> GetApariencia(CancellationToken cancellationToken)
        {
            Response.Headers.CacheControl = "no-cache";
            var result = await _mediator.Send(new GetAparienciaQuery(), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Logo de la apariencia activa. El frontend debe pedirlo como <c>logo?v={id}-{version}</c>: así el
        /// navegador puede guardarlo en caché y se descarga de nuevo solo cuando cambia.
        /// </summary>
        [HttpGet("apariencia/logo")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLogo(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetLogoQuery(), cancellationToken);
            if (result.IsFailure) return HandleFailure(result);

            Response.Headers.CacheControl = "public, max-age=86400";
            Response.Headers.ETag = $"\"logo-{result.Value.AparienciaId:N}-{result.Value.Version}\"";
            return Logo(result.Value);
        }

        /// <summary>Apariencias guardadas; la activa primero.</summary>
        [HttpGet("apariencias")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> GetApariencias(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAparienciasQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("apariencias/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> GetAparienciaById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAparienciaByIdQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Crea una apariencia inactiva; los usuarios la ven al activarla.</summary>
        [HttpPost("apariencias")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> CreateApariencia([FromBody] CreateAparienciaCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Si la apariencia está activa, los cambios se aplican de inmediato a todos los usuarios.</summary>
        [HttpPut("apariencias/{id:Guid}")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> UpdateApariencia([FromRoute] Guid id, [FromBody] DatosAparienciaRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateAparienciaCommand(id, request.Nombre, request.Descripcion, request.NombreSistema,
                request.NombreCorto, request.NombreInstitucion, request.ColorPrimario, request.ColorSecundario,
                request.ColorBotonPrimario, request.ColorBotonSecundario, request.ColorBotonPeligro);

            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Copia textos, colores y logo en una apariencia nueva (inactiva).</summary>
        [HttpPost("apariencias/{id:Guid}/duplicar")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> DuplicarApariencia([FromRoute] Guid id, [FromBody] DuplicarAparienciaRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DuplicarAparienciaCommand(id, request.Nombre), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Aplica esta apariencia a todos los usuarios; la anterior queda guardada.</summary>
        [HttpPost("apariencias/{id:Guid}/activar")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> ActivarApariencia([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ActivarAparienciaCommand(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Logo de una apariencia guardada, para su vista previa.</summary>
        [HttpGet("apariencias/{id:Guid}/logo")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> GetLogoApariencia([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetLogoAparienciaQuery(id), cancellationToken);
            return result.IsFailure ? HandleFailure(result) : Logo(result.Value);
        }

        [HttpPut("apariencias/{id:Guid}/logo")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> ActualizarLogo([FromRoute] Guid id, [FromBody] LogoAparienciaRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ActualizarLogoCommand(id, request.NombreArchivo, request.FileBase64), cancellationToken);
            return HandleResult(result);
        }

        [HttpDelete("apariencias/{id:Guid}/logo")]
        [AppAuthorize(AppPolicy.CanManageConfiguracion)]
        public async Task<IActionResult> EliminarLogo([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new EliminarLogoCommand(id), cancellationToken);
            return HandleResult(result);
        }

        private FileContentResult Logo(LogoArchivo logo)
        {
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(logo.Contenido, logo.ContentType);
        }
    }

    public record DatosAparienciaRequest(
        string Nombre,
        string? Descripcion,
        string NombreSistema,
        string? NombreCorto,
        string? NombreInstitucion,
        string ColorPrimario,
        string ColorSecundario,
        string ColorBotonPrimario,
        string ColorBotonSecundario,
        string ColorBotonPeligro);

    public record DuplicarAparienciaRequest(string Nombre);

    public record LogoAparienciaRequest(string NombreArchivo, string FileBase64);
}
