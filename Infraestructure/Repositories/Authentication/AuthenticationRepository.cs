using Application.Contracts.Authentication;
using Domain.Common;
using Domain.Common.Errors;
using Infraestructure.Identity.Authentication;
using Infraestructure.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Infraestructure.Repositories.Authentication
{
    public class AuthenticationRepository(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IJwtTokenGenerator tokenGenerator) : IAuthenticationRepository
    {
        public async Task<Result<string>> Login(string username, string password)
        {
            var user = await userManager.FindByNameAsync(username);

            if (user is null)
                return Result.Failure<string>(AuthenticationErrors.UserNotFound(username));

            var loginResult = await signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!loginResult.Succeeded)
            {
                return Result.Failure<string>(AuthenticationErrors.InvalidCredentials());
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = tokenGenerator.GenerateToken(user, [.. roles]);

            return Result.Success(token);
        }
    }
}
