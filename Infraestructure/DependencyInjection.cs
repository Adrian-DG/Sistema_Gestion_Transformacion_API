using Application.Contracts;
using Application.Contracts.Recursos;
using Infraestructure.Data;
using Infraestructure.Identity.Authentication;
using Infraestructure.Repositories.Recursos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infraestructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfraestructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<MainContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IVehiculoRepository, VehiculoRepository>();

            return services;
        }
    }
}
