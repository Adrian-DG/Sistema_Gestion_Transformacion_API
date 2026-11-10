using Application.Contracts;
using Domain.Common;
using FluentValidation;
using MediatR;

namespace Application.Features.Historico.Asignaciones;

/// <summary>
/// Advertencias para asignar el vehículo en el período (mantenimiento pendiente, póliza). El formulario las
/// muestra al elegir el vehículo y la aprobación las presenta antes de confirmar.
/// </summary>
public record GetAdvertenciasAsignacionQuery(Guid VehiculoId, DateTime Salida, DateTime RetornoEstimado)
    : IRequest<Result<IReadOnlyList<AdvertenciaAsignacion>>>;

public class GetAdvertenciasAsignacionQueryValidator : AbstractValidator<GetAdvertenciasAsignacionQuery>
{
    public GetAdvertenciasAsignacionQueryValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.RetornoEstimado).GreaterThan(x => x.Salida)
            .WithMessage("La fecha de retorno estimada debe ser posterior a la fecha de salida.");
    }
}

public class GetAdvertenciasAsignacionQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetAdvertenciasAsignacionQuery, Result<IReadOnlyList<AdvertenciaAsignacion>>>
{
    public async Task<Result<IReadOnlyList<AdvertenciaAsignacion>>> Handle(GetAdvertenciasAsignacionQuery request, CancellationToken cancellationToken) =>
        Result.Success(await uow.ObtenerAdvertenciasAsync(request.VehiculoId, request.Salida, request.RetornoEstimado, cancellationToken));
}
