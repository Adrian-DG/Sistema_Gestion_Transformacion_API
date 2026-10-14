using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Historico;
using Application.Features.Historico.Asignaciones;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Constants;
using Domain.Entities.Historico;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories.Historico
{
    public class AsignacionRepository(MainContext context) : IAsignacionRepository
    {
        public async Task Create(Asignacion asignacion, CancellationToken cancellationToken)
        {
            await context.Asignaciones.AddAsync(asignacion, cancellationToken);
        }

        /// <summary>Las de salida más reciente primero; busca por propósito, destino, dependencia, vehículo, nombres o cédulas.</summary>
        public async Task<PagedData<AsignacionViewModel>> Get(FiltroAsignacionesQuery filter, CancellationToken cancellationToken)
        {
            var query = context.Asignaciones.AsQueryable();

            if (filter.Estatus is { } estatus)
            {
                query = query.Where(x => x.Estatus == estatus);
            }

            if (!string.IsNullOrWhiteSpace(filter.Param))
            {
                query = query.Where(x =>
                    x.Proposito.Contains(filter.Param) ||
                    (x.Destino != null && x.Destino.Contains(filter.Param)) ||
                    x.DependenciaSolicitante.Contains(filter.Param) ||
                    (x.SolicitantePersona!.Nombre + " " + x.SolicitantePersona.Apellido).Contains(filter.Param) ||
                    (x.Persona!.Nombre + " " + x.Persona.Apellido).Contains(filter.Param) ||
                    (x.ChoferPersona!.Nombre + " " + x.ChoferPersona.Apellido).Contains(filter.Param) ||
                    x.SolicitantePersona!.Identificacion.Contains(filter.Param) ||
                    x.Persona!.Identificacion.Contains(filter.Param) ||
                    x.ChoferPersona!.Identificacion.Contains(filter.Param) ||
                    x.Vehiculo!.Placa.Contains(filter.Param) ||
                    x.Vehiculo.Matricula.Contains(filter.Param) ||
                    x.Vehiculo.Chasis.Contains(filter.Param));
            }

            var totalRecords = await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderByDescending(x => x.FechaSalida)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new AsignacionViewModel
                {
                    Id = x.Id,
                    Estatus = x.Estatus.ToString(),
                    Solicitante = x.SolicitantePersona!.Apellido + " " + x.SolicitantePersona.Nombre,
                    SolicitanteCedula = x.SolicitantePersona.Identificacion,
                    RangoSolicitante = x.RangoSolicitante,
                    CargoSolicitante = x.CargoSolicitante,
                    DependenciaSolicitante = x.DependenciaSolicitante,
                    TipoAsignacion = x.TipoAsignacion.ToString(),
                    AsignadoA = x.Persona!.Apellido + " " + x.Persona.Nombre,
                    AsignadoACedula = x.Persona.Identificacion,
                    CodCargo = x.CodCargo,
                    Cargo = x.Cargo,
                    Chofer = x.ChoferPersona!.Apellido + " " + x.ChoferPersona.Nombre,
                    ChoferCedula = x.ChoferPersona.Identificacion,
                    RangoChofer = x.RangoChofer,
                    Proposito = x.Proposito,
                    Destino = x.Destino,
                    VehiculoId = x.VehiculoId,
                    Chasis = x.Vehiculo!.Chasis,
                    Placa = x.Vehiculo.Placa,
                    Matricula = x.Vehiculo.Matricula,
                    Marca = x.Vehiculo.Marca!.Nombre,
                    Modelo = x.Vehiculo.Modelo!.Nombre,
                    FechaSalida = x.FechaSalida,
                    FechaRetornoEstimada = x.FechaRetornoEstimada,
                    FechaRetornoReal = x.FechaRetornoReal,
                    MotivoRechazoCancelacion = x.MotivoRechazoCancelacion,
                    CantidadAdjuntos = context.Adjuntos.Count(a =>
                        a.TipoOperacionId == TiposOperacion.Asignacion && a.IdentificadorOperacion == x.Id)
                })
                .ToListAsync(cancellationToken);

            return new PagedData<AsignacionViewModel>(rows, filter.PageSize, filter.PageNumber, totalRecords);
        }

        public async Task<Result<Asignacion>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var asignacion = await context.Asignaciones.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return asignacion is not null
                ? Result.Success(asignacion)
                : Result.Failure<Asignacion>(AsignacionErrors.NotFound(id));
        }
    }
}
