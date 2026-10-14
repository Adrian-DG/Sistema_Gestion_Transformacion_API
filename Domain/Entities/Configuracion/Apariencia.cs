using Domain.Abstraction;

namespace Domain.Entities.Configuracion
{
    /// <summary>
    /// Personalización visual del frontend (logo, nombre del sistema y colores). Puede haber varias
    /// apariencias guardadas, pero solo una activa (<see cref="EsActiva"/>), que es la que ven todos los
    /// usuarios. No se eliminan: se cambia cuál está activa. <see cref="Version"/> aumenta con cada cambio
    /// para que el frontend sepa cuándo refrescar lo que tiene guardado en el navegador.
    /// </summary>
    public class Apariencia : NamedEntityMetadata, IAuditableEntityMetadata
    {
        /// <summary>Apariencia institucional creada con la base de datos.</summary>
        public static readonly Guid IdInicial = new("3c5d8e2f-7a41-4b9e-9d16-2f0a00000001");

        public string? Descripcion { get; set; }

        /// <summary>Solo una apariencia puede estar activa.</summary>
        public bool EsActiva { get; set; }

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

        public const string NombreInicial = "Institucional";
        public const string NombreSistemaPredeterminado = "Sistema de Gestión de Transportación";
        public const string NombreCortoPredeterminado = "SGT";
        public const string NombreInstitucionPredeterminado = "Ministerio de Defensa · Dirección de Transportación (J-4)";
        public const string ColorPrimarioPredeterminado = "#002D62";
        public const string ColorSecundarioPredeterminado = "#C9A227";
        public const string ColorBotonPrimarioPredeterminado = "#002D62";
        public const string ColorBotonSecundarioPredeterminado = "#5B6B7F";
        public const string ColorBotonPeligroPredeterminado = "#CE1126";

        /// <summary>Apariencia institucional, activa; es la que se usa si la base no tiene ninguna.</summary>
        public static Apariencia Predeterminada() => new()
        {
            Id = IdInicial,
            Status = true,
            Nombre = NombreInicial,
            Descripcion = "Colores y nombre institucionales del Ministerio de Defensa.",
            EsActiva = true,
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

        public void CambiarLogo(byte[]? logo, string? contentType, string? nombreArchivo)
        {
            Logo = logo;
            LogoContentType = contentType;
            LogoNombreArchivo = nombreArchivo;
            Version++;
        }
    }
}
