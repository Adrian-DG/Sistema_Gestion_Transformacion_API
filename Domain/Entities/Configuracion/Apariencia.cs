using Domain.Abstraction;

namespace Domain.Entities.Configuracion
{
    /// <summary>
    /// Personalización visual del frontend (logo, nombre del sistema y colores). Existe un único
    /// registro, identificado por <see cref="IdUnico"/>; <see cref="Version"/> aumenta con cada cambio
    /// para que el frontend sepa cuándo refrescar lo que tiene guardado en el navegador.
    /// </summary>
    public class Apariencia : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public static readonly Guid IdUnico = new("3c5d8e2f-7a41-4b9e-9d16-2f0a00000001");

        /// <summary>Nombre mostrado en la pantalla de inicio de sesión y en la barra de navegación.</summary>
        public required string NombreSistema { get; set; }

        /// <summary>Versión abreviada para espacios reducidos (barra colapsada, móviles).</summary>
        public string? NombreCorto { get; set; }

        /// <summary>Institución o dependencia mostrada como subtítulo.</summary>
        public string? NombreInstitucion { get; set; }

        public byte[]? Logo { get; set; }
        public string? LogoContentType { get; set; }
        public string? LogoNombreArchivo { get; set; }

        /// <summary>Colores en formato HEX (#RRGGBB).</summary>
        public required string ColorPrimario { get; set; }
        public required string ColorSecundario { get; set; }
        public required string ColorBotonPrimario { get; set; }
        public required string ColorBotonSecundario { get; set; }
        public required string ColorBotonPeligro { get; set; }

        public int Version { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public const string NombreSistemaPredeterminado = "Sistema de Gestión de Transportación";
        public const string NombreCortoPredeterminado = "SGT";
        public const string NombreInstitucionPredeterminado = "Ministerio de Defensa · Dirección de Transportación (J-4)";
        public const string ColorPrimarioPredeterminado = "#002D62";
        public const string ColorSecundarioPredeterminado = "#C9A227";
        public const string ColorBotonPrimarioPredeterminado = "#002D62";
        public const string ColorBotonSecundarioPredeterminado = "#5B6B7F";
        public const string ColorBotonPeligroPredeterminado = "#CE1126";

        public static Apariencia Predeterminada() => new()
        {
            Id = IdUnico,
            Status = true,
            NombreSistema = NombreSistemaPredeterminado,
            NombreCorto = NombreCortoPredeterminado,
            NombreInstitucion = NombreInstitucionPredeterminado,
            ColorPrimario = ColorPrimarioPredeterminado,
            ColorSecundario = ColorSecundarioPredeterminado,
            ColorBotonPrimario = ColorBotonPrimarioPredeterminado,
            ColorBotonSecundario = ColorBotonSecundarioPredeterminado,
            ColorBotonPeligro = ColorBotonPeligroPredeterminado,
            Version = 1
        };

        /// <summary>Vuelve los textos y colores a los valores institucionales; el logo se conserva.</summary>
        public void Restablecer()
        {
            NombreSistema = NombreSistemaPredeterminado;
            NombreCorto = NombreCortoPredeterminado;
            NombreInstitucion = NombreInstitucionPredeterminado;
            ColorPrimario = ColorPrimarioPredeterminado;
            ColorSecundario = ColorSecundarioPredeterminado;
            ColorBotonPrimario = ColorBotonPrimarioPredeterminado;
            ColorBotonSecundario = ColorBotonSecundarioPredeterminado;
            ColorBotonPeligro = ColorBotonPeligroPredeterminado;
            Version++;
        }

        public void CambiarLogo(byte[]? logo, string? contentType, string? nombreArchivo)
        {
            Logo = logo;
            LogoContentType = contentType;
            LogoNombreArchivo = nombreArchivo;
            Version++;
        }
    }
}
