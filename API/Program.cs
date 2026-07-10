using API.Middleware;
using Application;
using Infraestructure;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Asegurar que Components esté inicializado
        document.Components ??= new OpenApiComponents();

        // Agregar esquema de seguridad Bearer JWT (Usando la clase concreta)
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Introduce tu token JWT sin la palabra 'Bearer'"
        });

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        // Ejemplo opcional de cómo agregar respuestas con ejemplos usando clases concretas
        if (operation.Responses.TryGetValue("200", out var response))
        {
            if (response.Content.TryGetValue("application/json", out var mediaType))
            {
                // Usamos el diccionario Examples para evitar problemas de solo lectura
                mediaType.Examples.Add("EjemploExitoso", new OpenApiExample
                {
                    Value = JsonNode.Parse("{ \"mensaje\": \"Operación exitosa\" }"),
                    Summary = "Respuesta estándar en formato JSON"
                });
            }
        }
        return Task.CompletedTask;
    });
});


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplicationServices();
builder.Services.AddInfraestructureServices(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(opt =>
    {
        opt.WithTitle("Dirección Transportación API");
        opt.AddPreferredSecuritySchemes("Bearer");
        opt.WithTheme(ScalarTheme.Mars);
        opt.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
