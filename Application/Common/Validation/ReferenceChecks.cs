using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Validation;

/// <summary>
/// Verificaciones de existencia de las personas que reciben o solicitan. Los cargos y dependencias
/// provienen de SIPFFAA y se guardan como código y nombre, por lo que no se validan aquí.
/// </summary>
public static class ReferenceChecks
{
    public static async Task<Result> ValidarBeneficiarioAsync(
        this IUnitOfWork uow, TipoBeneficiario tipo, Guid? personaId, CancellationToken cancellationToken)
    {
        return tipo == TipoBeneficiario.Persona
            ? await uow.ValidarPersonaAsync(personaId!.Value, cancellationToken)
            : Result.Success();
    }

    public static async Task<Result> ValidarPersonaAsync(this IUnitOfWork uow, Guid id, CancellationToken cancellationToken)
    {
        var exists = await uow.Query<Persona>().AnyAsync(p => p.Id == id && p.Status, cancellationToken);
        return exists ? Result.Success() : Result.Failure(PersonaErrors.NotFound(id));
    }
}

public static class BeneficiarioValidationExtensions
{
    /// <summary>
    /// Exige el cargo (código y nombre de SIPFFAA) cuando el tipo es Cargo y PersonaId cuando es Persona, nunca ambos.
    /// </summary>
    public static void ValidarBeneficiario<T>(
        this AbstractValidator<T> validator,
        Func<T, TipoBeneficiario> tipo,
        Func<T, int?> codCargo,
        Func<T, string?> cargo,
        Func<T, Guid?> personaId)
    {
        validator.RuleFor(x => tipo(x)).IsInEnum().OverridePropertyName("TipoBeneficiario");
        validator.RuleFor(x => cargo(x)).MaximumLength(200).OverridePropertyName("Cargo");

        validator.RuleFor(x => x)
            .Must(x => tipo(x) != TipoBeneficiario.Cargo
                || (codCargo(x) > 0 && !string.IsNullOrWhiteSpace(cargo(x)) && personaId(x) is null))
            .WithMessage("Para una asignación al cargo debe indicar CodCargo y Cargo, y no PersonaId.")
            .Must(x => tipo(x) != TipoBeneficiario.Persona
                || (personaId(x) is { } p && p != Guid.Empty && codCargo(x) is null && cargo(x) is null))
            .WithMessage("Para una asignación a persona debe indicar PersonaId, y no CodCargo ni Cargo.");
    }

    /// <summary>Dependencia solicitante tomada de SIPFFAA (CodDepartamento y Departamento).</summary>
    public static void ValidarDependencia<T>(
        this AbstractValidator<T> validator, Func<T, int?> codDependencia, Func<T, string?> dependencia)
    {
        validator.RuleFor(x => dependencia(x)).NotEmpty().MaximumLength(200)
            .OverridePropertyName("DependenciaSolicitante")
            .WithMessage("Debe indicar la dependencia solicitante.");
        validator.RuleFor(x => codDependencia(x)).GreaterThan(0)
            .OverridePropertyName("CodDependenciaSolicitante")
            .When(x => codDependencia(x).HasValue);
    }
}
