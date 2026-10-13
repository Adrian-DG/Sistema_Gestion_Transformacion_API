using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Organizacion.Personas;

public record CreatePersonaCommand(
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    TipoPersona TipoPersona,
    Guid? RangoId,
    InstitucionEnum? Institucion,
    Guid? DependenciaId,
    string? Telefono
) : IRequest<Result<Guid>>;

public class CreatePersonaCommandValidator : AbstractValidator<CreatePersonaCommand>
{
    public CreatePersonaCommandValidator()
    {
        RuleFor(x => x.Identificacion).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Sexo).IsInEnum();
        RuleFor(x => x.TipoPersona).IsInEnum();
        RuleFor(x => x.Telefono).MaximumLength(20);

        When(x => x.TipoPersona == TipoPersona.Militar, () =>
        {
            RuleFor(x => x.RangoId).NotEmpty().WithMessage("El rango es obligatorio para el personal militar.");
            RuleFor(x => x.Institucion).NotNull().IsInEnum().WithMessage("La institución es obligatoria para el personal militar.");
        });
    }
}

public class CreatePersonaCommandHandler(IUnitOfWork uow) : IRequestHandler<CreatePersonaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePersonaCommand request, CancellationToken cancellationToken)
    {
        var duplicada = await uow.Query<Persona>().AnyAsync(p => p.Identificacion == request.Identificacion, cancellationToken);
        if (duplicada)
        {
            return Result.Failure<Guid>(OrganizacionErrors.PersonaDuplicada(request.Identificacion));
        }

        if (request.DependenciaId.HasValue)
        {
            var dependencia = await uow.ValidarDependenciaAsync(request.DependenciaId.Value, cancellationToken);
            if (dependencia.IsFailure) return Result.Failure<Guid>(dependencia.Error);
        }

        var persona = new Persona
        {
            Identificacion = request.Identificacion,
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Sexo = request.Sexo,
            TipoPersona = request.TipoPersona,
            RangoId = request.RangoId,
            Institucion = request.Institucion,
            DependenciaId = request.DependenciaId,
            Telefono = request.Telefono,
            Status = true
        };

        await uow.AddAsync(persona, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(persona.Id);
    }
}
