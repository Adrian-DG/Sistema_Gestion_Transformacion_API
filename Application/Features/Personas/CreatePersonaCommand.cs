using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Personas;

public record CreatePersonaCommand(
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    TipoPersona TipoPersona,
    Guid? RangoId,
    InstitucionEnum? Institucion,
    int? CodDependencia,
    string? Dependencia,
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
        RuleFor(x => x.Dependencia).MaximumLength(200);
        RuleFor(x => x.CodDependencia).GreaterThan(0).When(x => x.CodDependencia.HasValue);
        RuleFor(x => x.Dependencia).NotEmpty().When(x => x.CodDependencia.HasValue)
            .WithMessage("Indique el nombre de la dependencia junto con su código.");

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
            return Result.Failure<Guid>(PersonaErrors.Duplicada(request.Identificacion));
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
            CodDependencia = request.CodDependencia,
            Dependencia = string.IsNullOrWhiteSpace(request.Dependencia) ? null : request.Dependencia.Trim(),
            Telefono = request.Telefono,
            Status = true
        };

        await uow.AddAsync(persona, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(persona.Id);
    }
}
