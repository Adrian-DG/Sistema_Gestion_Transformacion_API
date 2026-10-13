namespace Domain.Enums
{
    /// <summary>
    /// Reportes PDF del sistema. Cada plantilla puede personalizar el título de cada uno;
    /// si no lo hace, se usa <see cref="TiposReportePdf.TituloPredeterminado"/>.
    /// </summary>
    public enum TipoReportePdf
    {
        OperacionesCombustible = 1,
        Asignaciones = 2
    }

    public static class TiposReportePdf
    {
        public static string Nombre(TipoReportePdf tipo) => tipo switch
        {
            TipoReportePdf.OperacionesCombustible => "Operaciones de combustible",
            TipoReportePdf.Asignaciones => "Asignaciones de vehículos",
            _ => tipo.ToString()
        };

        public static string TituloPredeterminado(TipoReportePdf tipo) => tipo switch
        {
            TipoReportePdf.OperacionesCombustible => "Reporte de operaciones de combustible",
            TipoReportePdf.Asignaciones => "Reporte de asignaciones de vehículos",
            _ => tipo.ToString()
        };
    }

    /// <summary>Ubicación del logo en el encabezado del reporte.</summary>
    public enum PosicionLogo
    {
        Izquierda = 1,
        Centro = 2,
        Derecha = 3
    }
}
