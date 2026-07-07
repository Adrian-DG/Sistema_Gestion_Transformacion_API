using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(Behaviors.ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(assembly);
            services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

            return services;
        }
    }
}
