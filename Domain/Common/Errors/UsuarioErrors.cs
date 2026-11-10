namespace Domain.Common.Errors;

public static class UsuarioErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Usuario.NotFound",
        $"No se encontró el usuario con id '{id}'.");

    public static Error Inactivo => Error.Forbidden(
        "Usuario.Inactivo",
        "El usuario está desactivado. Contacte al administrador del sistema.");

    public static Error RolesInvalidos(IEnumerable<string> roles) => Error.Validation(
        "Usuario.RolesInvalidos",
        $"Los siguientes permisos no existen: {string.Join(", ", roles)}.");

    public static Error AccionSobreSiMismo => Error.Conflict(
        "Usuario.AccionSobreSiMismo",
        "No puede desactivarse ni quitarse el permiso de Administrador a sí mismo.");

    public static Error SoloAdministrador => Error.Forbidden(
        "Usuario.SoloAdministrador",
        "Solo un Administrador puede otorgar o quitar el permiso de Administrador, o modificar a un administrador.");

    public static Error UltimoAdministrador => Error.Conflict(
        "Usuario.UltimoAdministrador",
        "Debe quedar al menos un administrador activo en el sistema.");

    public static Error Identity(string codigo, IEnumerable<string> mensajes) => Error.Validation(
        $"Usuario.{codigo}",
        string.Join(" ", mensajes));
}
