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

        public async Task<PagedData<AsignacionViewModel>> Get(PaginationFilterQuery<AsignacionViewModel> filter, CancellationToken cancellationToken)
        {
            var query = context.Asignaciones.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Param))
            {
                query = query.Where(x =>
                    x.Proposito.Contains(filter.Param) ||
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
                    TipoAsignacion = x.TipoAsignacion.ToString(),
                    CargoId = x.CargoId,
                    Cargo = x.Cargo != null ? x.Cargo.Nombre : null,
                    Responsable = x.Persona != null
                        ? x.Persona.Apellido + " " + x.Persona.Nombre
                        : x.Cargo != null && x.Cargo.Titular != null
                            ? x.Cargo.Titular.Apellido + " " + x.Cargo.Titular.Nombre
                            : null,
                    Solicitante = x.SolicitantePersona!.Apellido + " " + x.SolicitantePersona.Nombre,
                    DependenciaSolicitante = x.DependenciaSolicitante!.Nombre,
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
