namespace Domain.Common.Errors;

public static class ConfiguracionErrors
{
    public static readonly Error LogoNoConfigurado = Error.NotFound(
        "Apariencia.LogoNoConfigurado",
        "No se ha configurado un logo institucional.");

    public static readonly Error FormatoLogoInvalido = Error.Validation(
        "Apariencia.FormatoLogoInvalido",
        "El logo debe ser una imagen PNG, JPEG o WebP.");
}
