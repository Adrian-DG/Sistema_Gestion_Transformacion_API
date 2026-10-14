namespace Domain.Common.Errors;

public static class AuthenticationErrors
{
    /// <summary>Usuario inexistente o contraseña incorrecta; no se distingue cuál de los dos.</summary>
    public static Error InvalidCredentials() => Error.Unauthorized(
        "Authentication.InvalidCredentials",
        "Usuario o contraseña incorrectos.");

    public static Error LoginFailed() => Error.Unauthorized(
        "Authentication.LoginFailed",
        "El inicio de sesión falló. Por favor, intente nuevamente.");
}
