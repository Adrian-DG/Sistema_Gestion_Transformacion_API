using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Combustible;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Combustible.Movimientos;

/// <summary>
/// Registra una salida de combustible a un vehículo asignado (<see cref="ConceptoMovimientoCombustible.DespachoVehiculoAsignado"/>)
/// o a un conjunto de vehículos/unidades de una misión (<see cref="ConceptoMovimientoCombustible.DespachoConjunto"/>).
/// Los canjes de tickets se registran con UtilizarTicketCommand.
/// </summary>
public record RegistrarDespachoCommand(
    ConceptoMovimientoCombustible Concepto,
    TipoCombustible TipoCombustible,
    decimal Galones,
    DateTime? Fecha,
    string Proposito,
    Guid? AsignacionId,
    string? DescripcionConjunto,
    int? CantidadUnidades,
    Guid? SolicitantePersonaId,
    int? CodDependenciaSolicitante,
    string? DependenciaSolicitante,
    Guid? ReceptorPersonaId,
    string? ReceptorNombre
) : IRequest<Result<Guid>>;

public class RegistrarDespachoCommandValidator : AbstractValidator<RegistrarDespachoCommand>
{
    public RegistrarDespachoCommandValidator()
    {
        RuleFor(x => x.Concepto)
            .Must(c => c is ConceptoMovimientoCombustible.DespachoVehiculoAsignado or ConceptoMovimientoCombustible.DespachoConjunto)
            .WithMessage("El concepto debe ser DespachoVehiculoAsignado o DespachoConjunto.");
        RuleFor(x => x.TipoCombustible).IsInEnum();
        RuleFor(x => x.Galones).GreaterThan(0).PrecisionScale(12, 2, true);
        RuleFor(x => x.Fecha).LessThanOrEqualTo(_ => DateTime.Now).WithMessage("La fecha no puede ser futura.");
        RuleFor(x => x.Proposito).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ReceptorNombre).MaximumLength(200);

        When(x => x.Concepto == ConceptoMovimientoCombustible.DespachoVehiculoAsignado, () =>
        {
            RuleFor(x => x.AsignacionId).NotEmpty().WithMessage("Debe indicar la asignación del vehículo.");
        });

        When(x => x.Concepto == ConceptoMovimientoCombustible.DespachoConjunto, () =>
        {
            RuleFor(x => x.DescripcionConjunto).NotEmpty().MaximumLength(500);
            RuleFor(x => x.CantidadUnidades).NotNull().GreaterThan(0);
            RuleFor(x => x.SolicitantePersonaId).NotEmpty().WithMessage("Debe indicar quién solicita el combustible.");
            this.ValidarDependencia(x => x.CodDependenciaSolicitante, x => x.DependenciaSolicitante);
        });
    }
}

public class RegistrarDespachoCommandHandler(IUnitOfWork uow) : IRequestHandler<RegistrarDespachoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarDespachoCommand request, CancellationToken cancellationToken)
    {
        var solicitantePersonaId = request.SolicitantePersonaId;
        var codDependenciaSolicitante = request.CodDependenciaSolicitante;
        var dependenciaSolicitante = request.DependenciaSolicitante?.Trim();

        if (request.Concepto == ConceptoMovimientoCombustible.DespachoVehiculoAsignado)
        {
            var asignacion = await uow.Query<Asignacion>()
                .Where(a => a.Id == request.AsignacionId)
                .Select(a => new { a.Estatus, a.SolicitantePersonaId, a.CodDependenciaSolicitante, a.DependenciaSolicitante })
                .FirstOrDefaultAsync(cancellationToken);

            if (asignacion is null)
            {
                return Result.Failure<Guid>(AsignacionErrors.NotFound(request.AsignacionId!.Value));
            }

            if (asignacion.Estatus != EstatusSolicitud.Aprobada)
            {
                return Result.Failure<Guid>(CombustibleErrors.AsignacionNoAprobada(request.AsignacionId!.Value));
            }

            // Si no se indica, el solicitante del combustible es el de la asignación.
            solicitantePersonaId ??= asignacion.SolicitantePersonaId;
            if (string.IsNullOrWhiteSpace(dependenciaSolicitante))
            {
                codDependenciaSolicitante = asignacion.CodDependenciaSolicitante;
                dependenciaSolicitante = asignacion.DependenciaSolicitante;
            }
        }
        else
        {
            var solicitante = await uow.ValidarPersonaAsync(solicitantePersonaId!.Value, cancellationToken);
            if (solicitante.IsFailure) return Result.Failure<Guid>(solicitante.Error);
        }

        if (request.ReceptorPersonaId.HasValue)
        {
            var receptor = await uow.ValidarPersonaAsync(request.ReceptorPersonaId.Value, cancellationToken);
            if (receptor.IsFailure) return Result.Failure<Guid>(receptor.Error);
        }

        var existencia = await uow.ObtenerExistenciaAsync(request.TipoCombustible, cancellationToken);
        if (existencia.DisponibleParaDespacho < request.Galones)
        {
            return Result.Failure<Guid>(CombustibleErrors.ExistenciaInsuficiente(
                request.TipoCombustible.ToString(), existencia.DisponibleParaDespacho, request.Galones));
        }

        var movimiento = new MovimientoCombustible
        {
            Concepto = request.Concepto,
            TipoCombustible = request.TipoCombustible,
            Galones = request.Galones,
            Fecha = request.Fecha ?? DateTime.Now,
            Proposito = request.Proposito,
            AsignacionId = request.Concepto == ConceptoMovimientoCombustible.DespachoVehiculoAsignado ? request.AsignacionId : null,
            DescripcionConjunto = request.Concepto == ConceptoMovimientoCombustible.DespachoConjunto ? request.DescripcionConjunto : null,
            CantidadUnidades = request.Concepto == ConceptoMovimientoCombustible.DespachoConjunto ? request.CantidadUnidades : null,
            SolicitantePersonaId = solicitantePersonaId,
            CodDependenciaSolicitante = codDependenciaSolicitante,
            DependenciaSolicitante = dependenciaSolicitante,
            ReceptorPersonaId = request.ReceptorPersonaId,
            ReceptorNombre = request.ReceptorNombre,
            Status = true
        };

        await uow.AddAsync(movimiento, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(movimiento.Id);
    }
}
