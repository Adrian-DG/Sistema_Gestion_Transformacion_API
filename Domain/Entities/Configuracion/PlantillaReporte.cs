using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Configuracion
{
    /// <summary>
    /// Datos institucionales de los reportes PDF: logos, texto de encabezado, nota al pie, firmante
    /// y título de cada reporte. Los reportes usan la plantilla predeterminada; para implementar el
    /// sistema en otra institución basta con crear su plantilla y marcarla como predeterminada.
    /// </summary>
    public class PlantillaReporte : NamedEntityMetadata, IAuditableEntityMetadata
    {
        public static readonly Guid IdInicial = new("5e7a1c3d-2b84-4f6e-a1d9-7c0b00000001");

        public string? Descripcion { get; set; }

        /// <summary>Solo una plantilla activa puede ser la predeterminada.</summary>
        public bool EsPredeterminada { get; set; }

        /// <summary>Texto genérico del encabezado (una línea por renglón).</summary>
        public string? TextoEncabezado { get; set; }

        public string? NotaPiePagina { get; set; }

        /// <summary>Quien firma los reportes (ej. el Encargado). Sin nombre, el reporte no lleva bloque de firma.</summary>
        public string? FirmanteNombre { get; set; }
        public string? FirmanteRango { get; set; }
        public string? FirmanteProfesion { get; set; }
        public string? FirmanteCargo { get; set; }

        public virtual ICollection<PlantillaReporteLogo> Logos { get; set; } = [];
        public virtual ICollection<PlantillaReporteTitulo> Titulos { get; set; } = [];

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }

    public class PlantillaReporteLogo : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public Guid PlantillaReporteId { get; set; }
        public virtual PlantillaReporte? PlantillaReporte { get; set; }
        public PosicionLogo Posicion { get; set; }
        public required byte[] Contenido { get; set; }
        public required string ContentType { get; set; }
        public required string NombreArchivo { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }

    /// <summary>Título personalizado de un reporte dentro de una plantilla.</summary>
    public class PlantillaReporteTitulo : BaseEntityMetadata
    {
        public Guid PlantillaReporteId { get; set; }
        public virtual PlantillaReporte? PlantillaReporte { get; set; }
        public TipoReportePdf TipoReporte { get; set; }
        public required string Titulo { get; set; }
        public string? Subtitulo { get; set; }
    }
}
