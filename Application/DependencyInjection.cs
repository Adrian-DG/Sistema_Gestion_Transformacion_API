using Application.Common.DTO;
using Application.Features.Combustible.Movimientos;
using Application.Features.Combustible.Tickets;
using Application.Features.Historico.Asignaciones;
using Application.Features.Personas;
using Application.Features.Historico.Polizas;
using Application.Features.Vehiculo;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            // Mensajes predeterminados de FluentValidation en español, sin depender de la cultura del servidor.
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

            // AddValidatorsFromAssembly only registers closed generic validators. PaginationFilterQueryValidator<T>
            // is open generic and the built-in container can't auto-wire IValidator<PaginationFilterQuery<T>> from
            // it, so each concrete paginated ViewModel needs an explicit registration.
            services.AddScoped<IValidator<PaginationFilterQuery<VehiculoViewModel>>, PaginationFilterQueryValidator<VehiculoViewModel>>();
            services.AddScoped<IValidator<PaginationFilterQuery<AsignacionViewModel>>, PaginationFilterQueryValidator<AsignacionViewModel>>();
            services.AddScoped<IValidator<PaginationFilterQuery<PolizaViewModel>>, PaginationFilterQueryValidator<PolizaViewModel>>();
            services.AddScoped<IValidator<PaginationFilterQuery<PersonaViewModel>>, PaginationFilterQueryValidator<PersonaViewModel>>();
            services.AddScoped<IValidator<PaginationFilterQuery<SolicitudTicketViewModel>>, PaginationFilterQueryValidator<SolicitudTicketViewModel>>();
            services.AddScoped<IValidator<PaginationFilterQuery<MovimientoCombustibleViewModel>>, PaginationFilterQueryValidator<MovimientoCombustibleViewModel>>();

            services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

            return services;
        }
    }
}
