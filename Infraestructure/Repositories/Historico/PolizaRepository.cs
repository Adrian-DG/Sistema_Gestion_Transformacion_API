

using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Historico;
using Application.Features.Historico.Polizas;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories.Historico
{
    public class PolizaRepository(MainContext context) : IPolizaRepository
    {
        public async Task Create(Poliza poliza, CancellationToken cancellationToken)
        {
            await context.Polizas.AddAsync(poliza, cancellationToken);
        }    

        public async Task<Result<Poliza>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var poliza = await context.Polizas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return poliza is not null
                ? Result.Success(poliza)
                : Result.Failure<Poliza>(PolizaErrors.NotFound(id));
        }

        /// <summary>Busca por número de póliza, placa, matrícula o aseguradora; las de vencimiento más próximo primero.</summary>
        public async Task<PagedData<PolizaViewModel>> Get(PaginationFilterQuery<PolizaViewModel> filter, CancellationToken cancellationToken)
        {
            var query = context.Polizas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Param))
            {
                var texto = filter.Param.Trim();
                query = query.Where(x =>
                    x.Numero.Contains(texto) ||
                    x.Vehiculo!.Placa.Contains(texto) ||
                    x.Vehiculo.Matricula.Contains(texto) ||
                    x.Aseguradora!.Nombre.Contains(texto));
            }

            var totalRecords = await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderByDescending(x => x.FechaVencimiento)
                .ThenBy(x => x.Numero)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new PolizaViewModel
                {
                    Id = x.Id,
                    VehiculoId = x.VehiculoId,
                    Placa = x.Vehiculo!.Placa,
                    Vehiculo = x.Vehiculo.Marca!.Nombre + " " + x.Vehiculo.Modelo!.Nombre + " (" + x.Vehiculo.Fabricacion + ")",
                    NumeroPoliza = x.Numero,
                    Aseguradora = x.Aseguradora!.Nombre,
                    FechaExpedicion = x.FechaExpedicion,
                    FechaEfectividad = x.FechaEfectividad,
                    FechaVencimiento = x.FechaVencimiento
                })
                .ToListAsync(cancellationToken);

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            foreach (var row in rows)
            {
                row.Cobertura = Poliza.Cobertura(row.FechaVencimiento, hoy).ToString();
                row.DiasRestantes = Poliza.DiasRestantes(row.FechaVencimiento, hoy);
            }

            return new PagedData<PolizaViewModel>(rows, filter.PageSize, filter.PageNumber, totalRecords);
        }
    }
}
