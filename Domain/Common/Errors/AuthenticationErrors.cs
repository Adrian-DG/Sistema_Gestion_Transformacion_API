namespace Domain.Common.Errors;

public static class AuthenticationErrors
{
    public static Error InvalidCredentials() => Error.Unauthorized(
        "Authentication.InvalidCredentials",
        "No se pudieron validar las credenciales proporcionadas.");

    public static Error UserNotFound(string username) => Error.NotFound(
        "Authentication.UserNotFound",
        $"El usuario o las credenciales proporcionadas no fueron encontrados.");

    public static Error LoginFailed() => Error.Unauthorized(
        "Authentication.LoginFailed",
        "El inicio de sesión falló. Por favor, intente nuevamente.");
}
