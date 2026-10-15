using Application.Features.Externos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Consultas a sistemas externos (SIPFFAA) para completar datos de personas y cargos. Las búsquedas
    /// (miembros, cargos, departamentos) devuelven primero lo ya registrado en el sistema
    /// (<c>origen = "Historico"</c>) y solo consultan SIPFFAA si no hay coincidencias o si se indica
    /// <c>soloSipffaa=true</c>.
    /// </summary>
    [Authorize]
    [Route("api/externos")]
    public class ExternosController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpGet("miembros/{cedula}")]
        public async Task<IActionResult> GetMiembro([FromRoute] string cedula, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMiembroByCedulaQuery(cedula), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Miembros activos para autocompletar: si <c>texto</c> son dígitos busca por inicio de cédula
        /// (con o sin guiones); si no, por nombre.
        /// </summary>
        [HttpGet("miembros")]
        public async Task<IActionResult> BuscarMiembros([FromQuery] string texto, [FromQuery] int maxResultados = 20, [FromQuery] bool soloSipffaa = false, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new BuscarMiembrosExternosQuery(texto, maxResultados, soloSipffaa), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("cargos")]
        public async Task<IActionResult> BuscarCargos([FromQuery] string nombre, [FromQuery] int maxResultados = 20, [FromQuery] bool soloSipffaa = false, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new BuscarCargosExternosQuery(nombre, maxResultados, soloSipffaa), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>Departamentos que solicitan asignaciones de vehículos o tickets de combustible.</summary>
        [HttpGet("departamentos")]
        public async Task<IActionResult> BuscarDepartamentos([FromQuery] string nombre, [FromQuery] int maxResultados = 20, [FromQuery] bool soloSipffaa = false, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new BuscarDepartamentosExternosQuery(nombre, maxResultados, soloSipffaa), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("departamentos/{codDepartamento:int}")]
        public async Task<IActionResult> GetDepartamento([FromRoute] int codDepartamento, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetDepartamentoExternoQuery(codDepartamento), cancellationToken);
            return HandleResult(result);
        }
    }
}
