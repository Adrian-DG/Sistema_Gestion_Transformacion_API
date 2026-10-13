using Application.Contracts;
using Domain.Common;
using Domain.Entities.Organizacion;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Organizacion.Dependencias;

public record CreateDependenciaCommand(string Nombre, string? Siglas, InstitucionEnum Institucion) : IRequest<Result<Guid>>;

public class CreateDependenciaCommandValidator : AbstractValidator<CreateDependenciaCommand>
{
    public CreateDependenciaCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Siglas).MaximumLength(20);
        RuleFor(x => x.Institucion).IsInEnum();
    }
}

public class CreateDependenciaCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateDependenciaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDependenciaCommand request, CancellationToken cancellationToken)
    {
        var dependencia = new Dependencia
        {
            Nombre = request.Nombre,
            Siglas = request.Siglas,
            Institucion = request.Institucion,
            Status = true
        };

        await uow.AddAsync(dependencia, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(dependencia.Id);
    }
}
