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
    [Route("api/authentication")]
    public class AuthController(IMediator mediator) : ApiControllerBase(mediator)
    {
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }
    }
}
