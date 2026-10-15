using Application.Contracts;
using Application.Contracts.Externos;
using Domain.Entities.Combustible;
using Domain.Entities.Historico;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Externos;

/// <summary>
/// Búsquedas en los datos ya registrados en el sistema (personas, asignaciones, solicitudes de tickets
/// y movimientos de combustible). Se consultan antes que SIPFFAA: devuelven los mismos DTO con
/// <c>Origen = Historico</c>. Cargos y departamentos solo se toman si se guardaron con su código de SIPFFAA.
/// </summary>
public static class BusquedaHistorico
{
    public static async Task<IReadOnlyList<MiembroExternoDto>> BuscarMiembrosHistoricoAsync(
        this IUnitOfWork uow, string texto, int maxResultados, CancellationToken cancellationToken)
    {
        var limpio = texto.Trim();
        var digitos = limpio.Replace("-", string.Empty);
        var esCedula = digitos.Length > 0 && digitos.All(char.IsDigit);

        var query = uow.Query<Persona>().Where(p => p.Status);
        query = esCedula
            ? query.Where(p => p.Identificacion.Replace("-", "").StartsWith(digitos))
            : query.Where(p => (p.Nombre + " " + p.Apellido).Contains(limpio) ||
                               (p.Apellido + " " + p.Nombre).Contains(limpio));

        var personas = await query
            .OrderBy(p => p.Apellido).ThenBy(p => p.Nombre)
            .Take(maxResultados)
            .Select(p => new
            {
                p.Id,
                p.Identificacion,
                p.Nombre,
                p.Apellido,
                p.Sexo,
                p.TipoPersona,
                p.Institucion,
                Rango = p.Rango != null ? p.Rango.Nombre : null,
                p.CodDependencia,
                p.Dependencia
            })
            .ToListAsync(cancellationToken);

        if (personas.Count == 0) return [];

        // Persona no guarda su cargo: se toma el último con que solicitó una asignación o tickets.
        var ids = personas.Select(p => p.Id).ToList();
        var cargosAsignacion = await uow.Query<Asignacion>()
            .Where(a => ids.Contains(a.SolicitantePersonaId))
            .Select(a => new { PersonaId = a.SolicitantePersonaId, Cod = a.CodCargoSolicitante, Nombre = a.CargoSolicitante, a.CreatedAt })
            .ToListAsync(cancellationToken);
        var cargosTickets = await uow.Query<SolicitudTicket>()
            .Where(s => ids.Contains(s.SolicitantePersonaId))
            .Select(s => new { PersonaId = s.SolicitantePersonaId, Cod = s.CodCargoSolicitante, Nombre = s.CargoSolicitante, s.CreatedAt })
            .ToListAsync(cancellationToken);
        var ultimoCargo = cargosAsignacion.Concat(cargosTickets)
            .GroupBy(c => c.PersonaId)
            .ToDictionary(g => g.Key, g => g.MaxBy(c => c.CreatedAt)!);

        return personas.Select(p =>
        {
            ultimoCargo.TryGetValue(p.Id, out var cargo);
            return new MiembroExternoDto
            {
                Origen = OrigenBusqueda.Historico,
                Cedula = p.Identificacion,
                PrimerNombre = p.Nombre,
                PrimerApellido = p.Apellido,
                NombreCompleto = $"{p.Nombre} {p.Apellido}",
                Sexo = p.Sexo == SexoEnum.Femenino ? "F" : "M",
                EsMilitar = p.TipoPersona == TipoPersona.Militar,
                SiglasInstitucion = p.Institucion?.ToString(),
                Rango = p.Rango,
                CodDepartamento = p.CodDependencia ?? 0,
                Departamento = p.Dependencia,
                CodCargo = cargo?.Cod ?? 0,
                Cargo = cargo?.Nombre,
                Status = true
            };
        }).ToList();
    }

    public static async Task<IReadOnlyList<CargoExternoDto>> BuscarCargosHistoricoAsync(
        this IUnitOfWork uow, string nombre, int maxResultados, CancellationToken cancellationToken)
    {
        var texto = nombre.Trim();

        var registrados = new List<(int Cod, string Nombre, DateTime Fecha)>();
        registrados.AddRange((await uow.Query<Asignacion>()
                .Where(a => a.CodCargoSolicitante != null && a.CargoSolicitante.Contains(texto))
                .Select(a => new { Cod = a.CodCargoSolicitante!.Value, Nombre = a.CargoSolicitante, a.CreatedAt })
                .ToListAsync(cancellationToken))
            .Select(c => (c.Cod, c.Nombre, c.CreatedAt)));
        registrados.AddRange((await uow.Query<Asignacion>()
                .Where(a => a.CodCargo != null && a.Cargo != null && a.Cargo.Contains(texto))
                .Select(a => new { Cod = a.CodCargo!.Value, Nombre = a.Cargo!, a.CreatedAt })
                .ToListAsync(cancellationToken))
            .Select(c => (c.Cod, c.Nombre, c.CreatedAt)));
        registrados.AddRange((await uow.Query<SolicitudTicket>()
                .Where(s => s.CodCargoSolicitante != null && s.CargoSolicitante.Contains(texto))
                .Select(s => new { Cod = s.CodCargoSolicitante!.Value, Nombre = s.CargoSolicitante, s.CreatedAt })
                .ToListAsync(cancellationToken))
            .Select(c => (c.Cod, c.Nombre, c.CreatedAt)));
        registrados.AddRange((await uow.Query<SolicitudTicket>()
                .Where(s => s.CodCargo != null && s.Cargo != null && s.Cargo.Contains(texto))
                .Select(s => new { Cod = s.CodCargo!.Value, Nombre = s.Cargo!, s.CreatedAt })
                .ToListAsync(cancellationToken))
            .Select(c => (c.Cod, c.Nombre, c.CreatedAt)));

        // Un código por cargo, con el nombre más reciente con que se registró.
        return registrados
            .GroupBy(c => c.Cod)
            .Select(g => g.MaxBy(c => c.Fecha))
            .OrderBy(c => c.Nombre)
            .Take(maxResultados)
            .Select(c => new CargoExternoDto { Origen = OrigenBusqueda.Historico, CodCargo = c.Cod, Nombre = c.Nombre })
            .ToList();
    }

    public static async Task<IReadOnlyList<DepartamentoExternoDto>> BuscarDepartamentosHistoricoAsync(
        this IUnitOfWork uow, string nombre, int maxResultados, CancellationToken cancellationToken)
    {
        var texto = nombre.Trim();

        var registrados = new List<(int Cod, string Nombre, DateTime Fecha)>();
        registrados.AddRange((await uow.Query<Persona>()
                .Where(p => p.CodDependencia != null && p.Dependencia != null && p.Dependencia.Contains(texto))
                .Select(p => new { Cod = p.CodDependencia!.Value, Nombre = p.Dependencia!, p.CreatedAt })
                .ToListAsync(cancellationToken))
            .Select(d => (d.Cod, d.Nombre, d.CreatedAt)));
        registrados.AddRange((await uow.Query<Asignacion>()
                .Where(a => a.CodDependenciaSolicitante != null && a.DependenciaSolicitante.Contains(texto))
                .Select(a => new { Cod = a.CodDependenciaSolicitante!.Value, Nombre = a.DependenciaSolicitante, a.CreatedAt })
                .ToListAsync(cancellationToken))
            .Select(d => (d.Cod, d.Nombre, d.CreatedAt)));
        registrados.AddRange((await uow.Query<SolicitudTicket>()
                .Where(s => s.CodDependenciaSolicitante != null && s.DependenciaSolicitante.Contains(texto))
                .Select(s => new { Cod = s.CodDependenciaSolicitante!.Value, Nombre = s.DependenciaSolicitante, s.CreatedAt })
                .ToListAsync(cancellationToken))
            .Select(d => (d.Cod, d.Nombre, d.CreatedAt)));
        registrados.AddRange((await uow.Query<MovimientoCombustible>()
                .Where(m => m.CodDependenciaSolicitante != null && m.DependenciaSolicitante != null && m.DependenciaSolicitante.Contains(texto))
                .Select(m => new { Cod = m.CodDependenciaSolicitante!.Value, Nombre = m.DependenciaSolicitante!, m.CreatedAt })
                .ToListAsync(cancellationToken))
            .Select(d => (d.Cod, d.Nombre, d.CreatedAt)));

        return registrados
            .GroupBy(d => d.Cod)
            .Select(g => g.MaxBy(d => d.Fecha))
            .OrderBy(d => d.Nombre)
            .Take(maxResultados)
            .Select(d => new DepartamentoExternoDto
            {
                Origen = OrigenBusqueda.Historico,
                CodDepartamento = d.Cod,
                Nombre = d.Nombre,
                Status = true
            })
            .ToList();
    }
}
