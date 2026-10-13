using API.Security;
using Application.Features.Configuracion;
using Application.Security;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Plantillas de los reportes PDF (logos, encabezado, pie de página, firmante y títulos).
    /// Solo el Administrador las mantiene; los reportes leen la predeterminada con
    /// <c>GET api/reportes/plantilla/{tipoReporte}</c>.
    /// </summary>
    [Route("api/plantillas-reporte")]
    [AppAuthorize(AppPolicy.CanManageConfiguracion)]
    public class PlantillasReporteController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPlantillasReporteQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPlantillaReporteByIdQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Reportes PDF del sistema y su título predeterminado.</summary>
        [HttpGet("tipos-reporte")]
        public async Task<IActionResult> GetTiposReporte(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTiposReporteQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePlantillaReporteCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] DatosPlantillaReporteRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdatePlantillaReporteCommand(id, request.Nombre, request.Descripcion, request.TextoEncabezado,
                request.NotaPiePagina, request.FirmanteNombre, request.FirmanteRango, request.FirmanteProfesion,
                request.FirmanteCargo, request.Titulos);

            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/duplicar")]
        public async Task<IActionResult> Duplicar([FromRoute] Guid id, [FromBody] DuplicarPlantillaRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DuplicarPlantillaReporteCommand(id, request.Nombre), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/predeterminar")]
        public async Task<IActionResult> Predeterminar([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new PredeterminarPlantillaReporteCommand(id), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/activar")]
        public async Task<IActionResult> Activar([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CambiarEstadoPlantillaReporteCommand(id, true), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id:Guid}/desactivar")]
        public async Task<IActionResult> Desactivar([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CambiarEstadoPlantillaReporteCommand(id, false), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Imagen del logo; <paramref name="posicion"/>: Izquierda, Centro o Derecha.</summary>
        [HttpGet("{id:Guid}/logos/{posicion}")]
        public async Task<IActionResult> GetLogo([FromRoute] Guid id, [FromRoute] PosicionLogo posicion, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetLogoPlantillaQuery(id, posicion), cancellationToken);
            if (result.IsFailure) return HandleFailure(result);

            Response.Headers.XContentTypeOptions = "nosniff";
            return File(result.Value.Contenido, result.Value.ContentType);
        }

        [HttpPut("{id:Guid}/logos/{posicion}")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> ActualizarLogo([FromRoute] Guid id, [FromRoute] PosicionLogo posicion, [FromBody] LogoPlantillaRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ActualizarLogoPlantillaCommand(id, posicion, request.NombreArchivo, request.FileBase64), cancellationToken);
            return HandleResult(result);
        }

        [HttpDelete("{id:Guid}/logos/{posicion}")]
        public async Task<IActionResult> EliminarLogo([FromRoute] Guid id, [FromRoute] PosicionLogo posicion, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new EliminarLogoPlantillaCommand(id, posicion), cancellationToken);
            return HandleResult(result);
        }
    }

    public record DatosPlantillaReporteRequest(
        string Nombre,
        string? Descripcion,
        string? TextoEncabezado,
        string? NotaPiePagina,
        string? FirmanteNombre,
        string? FirmanteRango,
        string? FirmanteProfesion,
        string? FirmanteCargo,
        List<TituloReporteRequest>? Titulos);

    public record DuplicarPlantillaRequest(string Nombre);

    public record LogoPlantillaRequest(string NombreArchivo, string FileBase64);
}
