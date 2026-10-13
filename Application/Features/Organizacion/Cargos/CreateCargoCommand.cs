using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Entities.Organizacion;
using FluentValidation;
using MediatR;

namespace Application.Features.Organizacion.Cargos;

public record CreateCargoCommand(string Nombre, Guid DependenciaId, Guid? TitularId) : IRequest<Result<Guid>>;

public class CreateCargoCommandValidator : AbstractValidator<CreateCargoCommand>
{
    public CreateCargoCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DependenciaId).NotEmpty();
    }
}

public class CreateCargoCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateCargoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCargoCommand request, CancellationToken cancellationToken)
    {
        var dependencia = await uow.ValidarDependenciaAsync(request.DependenciaId, cancellationToken);
        if (dependencia.IsFailure) return Result.Failure<Guid>(dependencia.Error);

        if (request.TitularId.HasValue)
        {
            var titular = await uow.ValidarPersonaAsync(request.TitularId.Value, cancellationToken);
            if (titular.IsFailure) return Result.Failure<Guid>(titular.Error);
        }

        var cargo = new Cargo
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            DependenciaId = request.DependenciaId,
            Status = true
        };

        cargo.CambiarTitular(request.TitularId, DateTime.Now);

        await uow.AddAsync(cargo, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(cargo.Id);
    }
}
