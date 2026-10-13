namespace Infraestructure.External.Sipffaa
{
    /// <summary>
    /// SQL contra db_sipffaa. Las columnas se devuelven con los nombres de
    /// <see cref="Application.Contracts.Externos.MiembroExternoDto"/> y
    /// <see cref="Application.Contracts.Externos.CargoExternoDto"/>.
    /// </summary>
    internal static class SipffaaQueries
    {
        /// <summary>
        /// Vista de miembros (entidad Miembros1 del sistema SIPFFAA). Confirmar el nombre real
        /// de la vista en la base: el sufijo "1" suele ser agregado por el generador de EF.
        /// </summary>
        private const string VistaMiembros = "dbo.Miembros1";

        /// <remarks>
        /// @Cedula llega sin guiones. Si la columna siempre se guarda sin guiones, quitar el REPLACE
        /// para que el filtro pueda usar el índice. Ante registros duplicados (reingresos) se prefiere
        /// el activo más reciente.
        /// </remarks>
        public const string MiembroPorCedula = $"""
            SELECT TOP (1)
                m.CodMiembro, m.Cedula,
                m.PrimerNombre, m.SegundoNombre, m.PrimerApellido, m.SegundoApellido,
                m.NombreApellidoCompleto AS NombreCompleto,
                m.sexo AS Sexo, m.EsMilitar,
                m.CodInstitucion, m.SiglasInstitucion, m.Institucion,
                m.CodRango, m.Rango, m.RangoAbreviado,
                m.CodDepartamento, m.Departamento,
                m.CodCargo, m.Cargo, m.DescripcionCargo,
                m.EstadoMiembro, m.LicenciaConducir, m.NoCarnet, m.Status
            FROM {VistaMiembros} m
            WHERE REPLACE(m.Cedula, '-', '') = @Cedula
            ORDER BY m.Status DESC, m.FechaCreacion DESC
            """;

        /// <summary>Tabla de cargos (entidad CARGO del sistema SIPFFAA).</summary>
        private const string TablaCargos = "dbo.CARGO";

        /// <remarks>Busca en el nombre y la descripción de los cargos activos, incluidos los vacantes.</remarks>
        public const string CargosPorNombre = $"""
            SELECT TOP (@MaxResultados)
                c.CodCargo, c.Nombre, c.Descripcion, c.CodInstitucion, c.EsEncargado, c.EsUnica,
                (SELECT COUNT(*) FROM {VistaMiembros} m WHERE m.CodCargo = c.CodCargo AND m.Status = 1) AS CantidadOcupantes
            FROM {TablaCargos} c
            WHERE c.Status = 1
              AND (c.Nombre LIKE @Patron OR c.Descripcion LIKE @Patron)
            ORDER BY c.Nombre
            """;

        /// <remarks>
        /// Miembros activos de los cargos indicados, limitados por cargo porque un cargo
        /// no único (ej. Ayudante) puede tener muchos ocupantes.
        /// </remarks>
        public const string OcupantesPorCargos = $"""
            SELECT CodCargo, CodMiembro, Cedula, NombreCompleto, Rango, Departamento
            FROM (
                SELECT
                    m.CodCargo, m.CodMiembro, m.Cedula,
                    m.NombreApellidoCompleto AS NombreCompleto,
                    m.RangoAbreviado AS Rango,
                    m.Departamento,
                    ROW_NUMBER() OVER (PARTITION BY m.CodCargo ORDER BY m.NoOrdenRango, m.NombreApellidoCompleto) AS Fila
                FROM {VistaMiembros} m
                WHERE m.Status = 1 AND m.CodCargo IN @CodCargos
            ) o
            WHERE o.Fila <= @MaxOcupantes
            ORDER BY o.CodCargo, o.Fila
            """;
    }
}
