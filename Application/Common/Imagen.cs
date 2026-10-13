namespace Application.Common;

/// <summary>Utilidades para imágenes recibidas en Base64 (logos).</summary>
public static class Imagen
{
    public const string Png = "image/png";
    public const string Jpeg = "image/jpeg";
    public const string WebP = "image/webp";

    /// <summary>Acepta Base64 puro o un data URL ("data:image/png;base64,...").</summary>
    public static byte[]? Decodificar(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;

        var coma = base64.IndexOf(',');
        if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && coma > 0)
        {
            base64 = base64[(coma + 1)..];
        }

        var buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64.Trim(), buffer, out var escritos) ? buffer[..escritos] : null;
    }

    /// <summary>
    /// Identifica el formato por su firma, sin confiar en el nombre ni en el tipo declarado.
    /// SVG queda excluido a propósito: puede contener scripts.
    /// </summary>
    public static string? DetectarContentType(byte[] contenido) => contenido switch
    {
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => Png,
        [0xFF, 0xD8, 0xFF, ..] => Jpeg,
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => WebP,
        _ => null
    };
}
