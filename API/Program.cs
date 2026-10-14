using API.Middleware;
using Application;
using Infraestructure;
using Infraestructure.Data.Seed.Demo;
using Infraestructure.Identity.Seed;
using API.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Documento OpenAPI que muestra Scalar: esquema Bearer y, por endpoint, los permisos que exige.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<AuthenticationDocumentTransformer>();
    options.AddOperationTransformer<AuthorizationOperationTransformer>();
});


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplicationServices();
builder.Services.AddInfraestructureServices(builder.Configuration);

// Orígenes del frontend (Cors:Origenes). Content-Disposition se expone para que el navegador
// pueda leer el nombre de los reportes Excel descargados.
const string CorsFrontend = "Frontend";
var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(CorsFrontend, policy => policy
    .WithOrigins(origenes)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Content-Disposition")));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(opt =>
    {
        opt.WithTitle("Dirección de Transportación (J-4) · API");
        opt.WithTheme(ScalarTheme.Laserwave);
        opt.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        opt.AddPreferredSecuritySchemes(AuthenticationDocumentTransformer.BearerScheme);
        // Conserva el token al recargar la documentación (solo se publica en Development).
        opt.EnablePersistentAuthentication();
    });

    // Al abrir la API en el navegador se muestra la documentación.
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseCors(CorsFrontend);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.Services.SeedAdministradorAsync();

// Datos de ejemplo (DatosDemo:Habilitado); nunca en producción.
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDatosDemoAsync();
}

app.Run();
