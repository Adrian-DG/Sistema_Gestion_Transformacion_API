using Domain.Enums;

namespace Domain.Common.Errors;

public static class PlantillaReporteErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "PlantillaReporte.NotFound",
        $"No se encontró la plantilla de reporte con id '{id}'.");

    public static Error NombreDuplicado(string nombre) => Error.Conflict(
        "PlantillaReporte.NombreDuplicado",
        $"Ya existe una plantilla de reporte llamada '{nombre}'.");

    public static readonly Error SinPredeterminada = Error.NotFound(
        "PlantillaReporte.SinPredeterminada",
        "No hay una plantilla de reporte predeterminada configurada.");

    public static readonly Error PredeterminadaNoDesactivable = Error.Conflict(
        "PlantillaReporte.PredeterminadaNoDesactivable",
        "No se puede desactivar la plantilla predeterminada. Marque otra como predeterminada primero.");

    public static readonly Error InactivaNoPredeterminable = Error.Conflict(
        "PlantillaReporte.Inactiva",
        "Una plantilla inactiva no puede ser la predeterminada. Actívela primero.");

    public static Error LogoNoEncontrado(PosicionLogo posicion) => Error.NotFound(
        "PlantillaReporte.LogoNoEncontrado",
        $"La plantilla no tiene logo en la posición '{posicion}'.");

    public static readonly Error FormatoLogoInvalido = Error.Validation(
        "PlantillaReporte.FormatoLogoInvalido",
        "El logo de los reportes debe ser una imagen PNG o JPEG.");
}
