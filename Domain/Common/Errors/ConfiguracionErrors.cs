namespace Domain.Common.Errors;

public static class ConfiguracionErrors
{
    public static readonly Error LogoNoConfigurado = Error.NotFound(
        "Apariencia.LogoNoConfigurado",
        "No se ha configurado un logo institucional.");

    public static readonly Error FormatoLogoInvalido = Error.Validation(
        "Apariencia.FormatoLogoInvalido",
        "El logo debe ser una imagen PNG, JPEG o WebP.");

    public static Error AparienciaNotFound(Guid id) => Error.NotFound(
        "Apariencia.NotFound",
        $"No se encontró la apariencia con id '{id}'.");

    public static Error NombreDuplicado(string nombre) => Error.Conflict(
        "Apariencia.NombreDuplicado",
        $"Ya existe una apariencia llamada '{nombre}'.");
}
