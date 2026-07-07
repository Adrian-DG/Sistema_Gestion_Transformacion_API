

using Application.Constants;
using Infraestructure.Identity.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infraestructure.Identity.Authentication
{
    public class JwtTokenGenerator(IConfiguration configuration): IJwtTokenGenerator
    {
        public string GenerateToken(AppUser user, string[] permissions)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[JWTPropConstants.SECRET_KEY]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtClaimConstants.USERNAME, user.UserName ?? string.Empty),
                new Claim(JwtClaimConstants.INSTITUCION, $"{user.Institucion}"),
                new Claim(JwtClaimConstants.Permissions, string.Join(",", permissions ?? Array.Empty<string>()))
            };

            var token = new JwtSecurityToken(
                issuer: configuration[JWTPropConstants.ISSUER],
                audience: configuration[JWTPropConstants.AUDIENCE],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(configuration[JWTPropConstants.EXPIRY_IN_MINUTES]!)),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }        
    }
}
