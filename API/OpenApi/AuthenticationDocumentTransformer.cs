using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace API.OpenApi;

/// <summary>
/// Datos generales del documento y esquema de autenticación JWT (Bearer). El esquema no se exige de forma
/// global: <see cref="AuthorizationOperationTransformer"/> lo agrega solo a los endpoints protegidos.
/// </summary>
public sealed class AuthenticationDocumentTransformer : IOpenApiDocumentTransformer
{
    public const string BearerScheme = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Sistema de Gestión de Transportación",
            Version = "v1",
            Description = """
                API de la Dirección de Transportación (J-4) del Ministerio de Defensa: vehículos, asignaciones,
                pólizas, combustible, reportes y administración del sistema.

                ## Autenticación
                1. Envíe usuario y contraseña a `POST /api/authentication`; la respuesta es el token JWT.
                2. En **Authentication**, elija **Bearer** y pegue el token (sin la palabra `Bearer`).
                3. El token se conserva al recargar esta página y vence según la configuración `Expiration` (minutos).

                ## Autorización
                Cada endpoint protegido indica los permisos que acepta. El permiso **Administrador** tiene acceso
                a todo. Sin token se responde **401**; con un token sin el permiso requerido, **403**.
                """
        };

        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token obtenido en POST /api/authentication. Introdúzcalo sin la palabra 'Bearer'."
        };

        return Task.CompletedTask;
    }
}
