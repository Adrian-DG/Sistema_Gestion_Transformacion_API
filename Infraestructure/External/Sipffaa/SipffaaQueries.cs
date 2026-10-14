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
                {ColumnasMiembro}
            FROM {VistaMiembros} m
            WHERE REPLACE(m.Cedula, '-', '') = @Cedula
            ORDER BY m.Status DESC, m.FechaCreacion DESC
            """;

        /// <summary>Columnas de <see cref="Application.Contracts.Externos.MiembroExternoDto"/>; requiere el alias m.</summary>
        private const string ColumnasMiembro = """
            m.CodMiembro, m.Cedula,
            m.PrimerNombre, m.SegundoNombre, m.PrimerApellido, m.SegundoApellido,
            m.NombreApellidoCompleto AS NombreCompleto,
            m.sexo AS Sexo, m.EsMilitar,
            m.CodInstitucion, m.SiglasInstitucion, m.Institucion,
            m.CodRango, m.Rango, m.RangoAbreviado,
            m.CodDepartamento, m.Departamento,
            m.CodCargo, m.Cargo, m.DescripcionCargo,
            m.EstadoMiembro, m.LicenciaConducir, m.NoCarnet, m.Status
            """;

        /// <remarks>
        /// Miembros activos cuya cédula empieza por @CedulaPrefijo (solo dígitos) o cuyo nombre contiene
        /// @Patron; el servicio envía uno u otro según el texto buscado. Ordena por rango (NoOrdenRango) y nombre.
        /// </remarks>
        public const string MiembrosPorTexto = $"""
            SELECT TOP (@MaxResultados)
                {ColumnasMiembro}
            FROM {VistaMiembros} m
            WHERE m.Status = 1
              AND ((@CedulaPrefijo IS NOT NULL AND REPLACE(m.Cedula, '-', '') LIKE @CedulaPrefijo + '%')
                OR (@Patron IS NOT NULL AND m.NombreApellidoCompleto LIKE @Patron))
            ORDER BY m.NoOrdenRango, m.NombreApellidoCompleto
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

        /// <summary>Tabla de departamentos (entidad DEPARTAMENTO del sistema SIPFFAA).</summary>
        private const string TablaDepartamentos = "dbo.DEPARTAMENTO";

        /// <summary>
        /// Columnas de <see cref="Application.Contracts.Externos.DepartamentoExternoDto"/> con el nombre del
        /// departamento padre. La raíz de la estructura puede no tener padre (o apuntarse a sí misma).
        /// </summary>
        private const string ColumnasDepartamento = """
            d.CodDepartamento, d.Nombre, d.Descripcion, d.CodInstitucion, d.CodigoDeRegistro,
            d.CodDptoPadre, p.Nombre AS DepartamentoPadre,
            d.EstaEnElRecintoMilitar, d.Status
            """;

        private const string FromDepartamentos = $"""
            FROM {TablaDepartamentos} d
            LEFT JOIN {TablaDepartamentos} p ON p.CodDepartamento = d.CodDptoPadre AND p.CodDepartamento <> d.CodDepartamento
            """;

        /// <remarks>Busca en el nombre, la descripción y NameToSearch de los departamentos activos.</remarks>
        public const string DepartamentosPorNombre = $"""
            SELECT TOP (@MaxResultados)
                {ColumnasDepartamento}
            {FromDepartamentos}
            WHERE d.Status = 1
              AND (d.Nombre LIKE @Patron OR d.Descripcion LIKE @Patron OR d.NameToSearch LIKE @Patron)
            ORDER BY d.Nombre
            """;

        public const string DepartamentoPorCodigo = $"""
            SELECT
                {ColumnasDepartamento}
            {FromDepartamentos}
            WHERE d.CodDepartamento = @CodDepartamento
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
