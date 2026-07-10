using Infraestructure.Identity.Authentication;
using Infraestructure.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/authentication")]
    public class AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IJwtTokenGenerator tokenGenerator) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await userManager.FindByNameAsync(request.UserName);
            if (user is null)
            {
                return Unauthorized("Credenciales inválidas.");
            }

            var loginResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!loginResult.Succeeded)
            {
                return Unauthorized("Credenciales inválidas.");
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = tokenGenerator.GenerateToken(user, [.. roles]);

            return Ok(new LoginResponse(token));
        }
    }

    public record LoginRequest(string UserName, string Password);
    public record LoginResponse(string AccessToken);
}
