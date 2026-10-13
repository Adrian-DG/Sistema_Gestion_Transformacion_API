namespace Application.Contracts.Reportes;

/// <summary>Genera un libro de Excel a partir de hojas tabulares.</summary>
public interface IExcelReportGenerator
{
    byte[] Generar(LibroReporte libro);
}

/// <param name="Titulo">Encabezado de todas las hojas (ej. "Reporte de combustible").</param>
/// <param name="Subtitulo">Normalmente el período del reporte.</param>
public record LibroReporte(string Titulo, string Subtitulo, IReadOnlyList<HojaReporte> Hojas);

/// <param name="Nombre">Nombre de la pestaña (máx. 31 caracteres).</param>
/// <param name="Filas">Valores admitidos: texto, números, fechas (DateTime/DateOnly), bool y null.</param>
/// <param name="ColumnasTotalizables">Índices de las columnas numéricas que se suman en la fila de totales.</param>
public record HojaReporte(
    string Nombre,
    string Titulo,
    IReadOnlyList<string> Encabezados,
    IReadOnlyList<object?[]> Filas,
    IReadOnlyList<int>? ColumnasTotalizables = null);

public record ArchivoReporte(byte[] Contenido, string NombreArchivo)
{
    public const string ContentTypeExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
