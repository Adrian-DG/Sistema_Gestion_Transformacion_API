using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace API.OpenApi;

/// <summary>
/// Documenta la seguridad de cada endpoint a partir de sus atributos [Authorize]/[AppAuthorize]: exige el
/// token Bearer, agrega las respuestas 401/403 y describe los permisos aceptados (las políticas
/// <see cref="Application.Security.AppPolicy"/> se resuelven a sus roles). Los endpoints sin [Authorize]
/// o con [AllowAnonymous] quedan públicos.
/// </summary>
public sealed class AuthorizationOperationTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any()) return;

        var authorizeData = metadata.OfType<IAuthorizeData>().ToList();
        if (authorizeData.Count == 0) return;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(AuthenticationDocumentTransformer.BearerScheme, context.Document)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "No autenticado: falta el token o venció." });

        // Cada atributo es un requisito adicional (todos deben cumplirse); dentro de uno, basta cualquiera de sus roles.
        var policyProvider = context.ApplicationServices.GetRequiredService<IAuthorizationPolicyProvider>();
        var requisitos = new List<IReadOnlyList<string>>();
        foreach (var data in authorizeData)
        {
            var roles = await RolesAsync(data, policyProvider);
            if (roles.Count > 0) requisitos.Add(roles);
        }

        if (requisitos.Count == 0)
        {
            AgregarDescripcion(operation, "**Requiere autenticación.** Disponible para cualquier usuario autenticado.");
            return;
        }

        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "El usuario no tiene el permiso requerido." });
        AgregarDescripcion(operation,
            $"**Requiere autenticación y permiso:** {string.Join(" **y** ", requisitos.Select(r => string.Join(" o ", r.Select(x => $"`{x}`"))))}.");

        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        operation.Extensions["x-badges"] = new JsonNodeExtension(new JsonArray(requisitos
            .Select(r => (JsonNode)new JsonObject
            {
                ["name"] = string.Join(" / ", r),
                ["position"] = "after",
                ["color"] = "#C9A227"
            })
            .ToArray()));
    }

    private static async Task<IReadOnlyList<string>> RolesAsync(IAuthorizeData data, IAuthorizationPolicyProvider policyProvider)
    {
        var roles = new List<string>();

        if (!string.IsNullOrWhiteSpace(data.Roles))
        {
            roles.AddRange(data.Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        if (!string.IsNullOrWhiteSpace(data.Policy) && await policyProvider.GetPolicyAsync(data.Policy) is { } policy)
        {
            roles.AddRange(policy.Requirements.OfType<RolesAuthorizationRequirement>().SelectMany(r => r.AllowedRoles));
        }

        // Administrador al final: es el permiso que siempre aplica y lo relevante es el específico.
        return roles.Distinct()
            .OrderBy(r => r == Application.Security.AppPermission.Administrador.ToString())
            .ThenBy(r => r)
            .ToList();
    }

    private static void AgregarDescripcion(OpenApiOperation operation, string texto) =>
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? texto
            : $"{operation.Description}\n\n{texto}";
}
