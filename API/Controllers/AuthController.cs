using API.Security;
using Application.Contracts.Authentication;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/authentication")]
    public class AuthController(IAuthenticationRepository authenticationRepository) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await authenticationRepository.Login(request.UserName, request.Password);

            if (result.IsFailure)
            {
                return MapErrorToResponse(result.Error);
            }

            return Ok(new LoginResponse(result.Value));
        }

        private IActionResult MapErrorToResponse(Error error)
        {
            return error.Type switch
            {
                ErrorType.NotFound => NotFound(new { error = error.Message, code = error.Code }),
                ErrorType.Unauthorized => Unauthorized(new { error = error.Message, code = error.Code }),
                ErrorType.Validation => BadRequest(new { error = error.Message, code = error.Code }),
                ErrorType.Conflict => Conflict(new { error = error.Message, code = error.Code }),
                ErrorType.Forbidden => Forbid(),
                _ => StatusCode(500, new { error = error.Message, code = error.Code })
            };
        }
    }

    public record LoginRequest(string UserName, string Password);
    public record LoginResponse(string AccessToken);
}
