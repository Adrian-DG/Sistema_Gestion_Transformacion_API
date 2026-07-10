using Application.Constants;
using Application.Contracts;
using Application.Contracts.Historico;
using Application.Contracts.Misc;
using Application.Contracts.Recursos;
using Application.Security;
using Infraestructure.Data;
using Infraestructure.Identity.Authentication;
using Infraestructure.Identity.Models;
using Infraestructure.Interceptors;
using Infraestructure.Repositories.Historico;
using Infraestructure.Repositories.Misc;
using Infraestructure.Repositories.Recursos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Infraestructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfraestructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<AuditableInterceptor>();

            services.AddDbContext<MainContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.AddInterceptors(serviceProvider.GetRequiredService<AuditableInterceptor>());
            });

            services
                .AddIdentityCore<AppUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<AppRole>()
                .AddEntityFrameworkStores<MainContext>()
                .AddSignInManager();

            var key = configuration[JWTPropConstants.SECRET_KEY] ?? throw new InvalidOperationException("JWT secret key is not configured.");

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration[JWTPropConstants.ISSUER],
                        ValidAudience = configuration[JWTPropConstants.AUDIENCE],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AppPolicy.CanManageVehiculos.ToString(),
                    p => p.RequireRole(AppPermission.GestionVehiculos.ToString(), AppPermission.Administrador.ToString()));
                options.AddPolicy(AppPolicy.CanManageAsignaciones.ToString(),
                    p => p.RequireRole(AppPermission.GestionAsignaciones.ToString(), AppPermission.Administrador.ToString()));
                options.AddPolicy(AppPolicy.CanManagePolizas.ToString(),
                    p => p.RequireRole(AppPermission.GestionPolizas.ToString(), AppPermission.Administrador.ToString()));
            });

            services.AddMemoryCache();
            services.AddScoped(typeof(ICatalogRepository<>), typeof(CatalogRepository<>));

            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IVehiculoRepository, VehiculoRepository>();
            services.AddScoped<IAsignacionRepository, AsignacionRepository>();
            services.AddScoped<IPolizaRepository, PolizaRepository>();

            return services;
        }
    }
}
