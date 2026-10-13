using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Entities.Organizacion;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Validation;

/// <summary>
/// Verificaciones de existencia de personas, cargos y dependencias que reciben las solicitudes.
/// </summary>
public static class ReferenceChecks
{
    public static async Task<Result> ValidarBeneficiarioAsync(
        this IUnitOfWork uow, TipoBeneficiario tipo, Guid? cargoId, Guid? personaId, CancellationToken cancellationToken)
    {
        return tipo == TipoBeneficiario.Cargo
            ? await uow.ValidarCargoAsync(cargoId!.Value, cancellationToken)
            : await uow.ValidarPersonaAsync(personaId!.Value, cancellationToken);
    }

    public static async Task<Result> ValidarSolicitanteAsync(
        this IUnitOfWork uow, Guid personaId, Guid dependenciaId, CancellationToken cancellationToken)
    {
        var persona = await uow.ValidarPersonaAsync(personaId, cancellationToken);
        if (persona.IsFailure) return persona;

        return await uow.ValidarDependenciaAsync(dependenciaId, cancellationToken);
    }

    public static async Task<Result> ValidarPersonaAsync(this IUnitOfWork uow, Guid id, CancellationToken cancellationToken)
    {
        var exists = await uow.Query<Persona>().AnyAsync(p => p.Id == id && p.Status, cancellationToken);
        return exists ? Result.Success() : Result.Failure(OrganizacionErrors.PersonaNotFound(id));
    }

    public static async Task<Result> ValidarCargoAsync(this IUnitOfWork uow, Guid id, CancellationToken cancellationToken)
    {
        var exists = await uow.Query<Cargo>().AnyAsync(c => c.Id == id && c.Status, cancellationToken);
        return exists ? Result.Success() : Result.Failure(OrganizacionErrors.CargoNotFound(id));
    }

    public static async Task<Result> ValidarDependenciaAsync(this IUnitOfWork uow, Guid id, CancellationToken cancellationToken)
    {
        var exists = await uow.Query<Dependencia>().AnyAsync(d => d.Id == id && d.Status, cancellationToken);
        return exists ? Result.Success() : Result.Failure(OrganizacionErrors.DependenciaNotFound(id));
    }
}

public static class BeneficiarioValidationExtensions
{
    /// <summary>
    /// Exige CargoId cuando el tipo es Cargo y PersonaId cuando es Persona, nunca ambos.
    /// </summary>
    public static void ValidarBeneficiario<T>(
        this AbstractValidator<T> validator,
        Func<T, TipoBeneficiario> tipo,
        Func<T, Guid?> cargoId,
        Func<T, Guid?> personaId)
    {
        validator.RuleFor(x => tipo(x)).IsInEnum().OverridePropertyName("TipoBeneficiario");

        validator.RuleFor(x => x)
            .Must(x => tipo(x) != TipoBeneficiario.Cargo || (cargoId(x) is { } c && c != Guid.Empty && personaId(x) is null))
            .WithMessage("Para una asignación al cargo debe indicar CargoId y no PersonaId.")
            .Must(x => tipo(x) != TipoBeneficiario.Persona || (personaId(x) is { } p && p != Guid.Empty && cargoId(x) is null))
            .WithMessage("Para una asignación a persona debe indicar PersonaId y no CargoId.");
    }
}
