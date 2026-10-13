using API.Security;
using Application.Contracts.Reportes;
using Application.Features.Reportes.Asignaciones;
using Application.Features.Reportes.Combustible;
using Application.Security;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Estadísticas (JSON) y reportes Excel. Período: <c>periodo=Diario|Semanal|Mensual</c> con <c>fecha</c>
    /// opcional (por defecto hoy), o <c>periodo=Rango</c> con <c>desde</c> y <c>hasta</c>.
    /// </summary>
    [Route("api/reportes")]
    public class ReportesController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet("combustible/estadisticas")]
        [AppAuthorize(AppPolicy.CanManageCombustible)]
        public async Task<IActionResult> EstadisticasCombustible([FromQuery] GetEstadisticasCombustibleQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("combustible/excel")]
        [AppAuthorize(AppPolicy.CanManageCombustible)]
        public async Task<IActionResult> ExcelCombustible([FromQuery] GetReporteExcelCombustibleQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return ArchivoExcel(result);
        }

        [HttpGet("asignaciones/estadisticas")]
        [AppAuthorize(AppPolicy.CanManageAsignaciones)]
        public async Task<IActionResult> EstadisticasAsignaciones([FromQuery] GetEstadisticasAsignacionesQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("asignaciones/excel")]
        [AppAuthorize(AppPolicy.CanManageAsignaciones)]
        public async Task<IActionResult> ExcelAsignaciones([FromQuery] GetReporteExcelAsignacionesQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return ArchivoExcel(result);
        }

        private IActionResult ArchivoExcel(Result<ArchivoReporte> result) =>
            result.IsSuccess
                ? File(result.Value.Contenido, ArchivoReporte.ContentTypeExcel, result.Value.NombreArchivo)
                : HandleFailure(result);
    }
}
