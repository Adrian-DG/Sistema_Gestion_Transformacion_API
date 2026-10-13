namespace Application.Contracts.Externos;

/// <summary>
/// Consultas de solo lectura al sistema SIPFFAA (personal militar y cargos).
/// </summary>
public interface ISipffaaService
{
    Task<MiembroExternoDto?> GetMiembroByCedulaAsync(string cedula, CancellationToken cancellationToken);
    Task<IReadOnlyList<CargoExternoDto>> BuscarCargosAsync(string nombre, int maxResultados, CancellationToken cancellationToken);
}

/// <summary>
/// Datos de un miembro necesarios para registrar solicitantes, beneficiarios y titulares.
/// Se omiten intencionalmente los datos personales sensibles de la vista (sueldos, salud, rasgos físicos).
/// </summary>
public class MiembroExternoDto
{
    public int CodMiembro { get; set; }
    public string Cedula { get; set; } = string.Empty;
    public string PrimerNombre { get; set; } = string.Empty;
    public string? SegundoNombre { get; set; }
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Sexo { get; set; }
    public bool EsMilitar { get; set; }

    public int CodInstitucion { get; set; }
    public string? SiglasInstitucion { get; set; }
    public string? Institucion { get; set; }

    public int CodRango { get; set; }
    public string? Rango { get; set; }
    public string? RangoAbreviado { get; set; }

    public int CodDepartamento { get; set; }
    public string? Departamento { get; set; }

    public int CodCargo { get; set; }
    public string? Cargo { get; set; }
    public string? DescripcionCargo { get; set; }

    public string? EstadoMiembro { get; set; }
    public string? LicenciaConducir { get; set; }
    public string? NoCarnet { get; set; }
    public bool Status { get; set; }
}

/// <summary>Cargo de SIPFFAA (tabla CARGO) con los miembros activos que lo ocupan.</summary>
public class CargoExternoDto
{
    public int CodCargo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CodInstitucion { get; set; }
    public bool EsEncargado { get; set; }
    public bool? EsUnica { get; set; }

    /// <summary>Total de miembros activos en el cargo; <see cref="Ocupantes"/> trae como máximo los primeros.</summary>
    public int CantidadOcupantes { get; set; }
    public List<OcupanteCargoExternoDto> Ocupantes { get; set; } = [];
}

public class OcupanteCargoExternoDto
{
    public int CodCargo { get; set; }
    public int CodMiembro { get; set; }
    public string Cedula { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Rango { get; set; }
    public string? Departamento { get; set; }
}
