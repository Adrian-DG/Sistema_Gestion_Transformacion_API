using Application.Common.Validation;
using Application.Contracts;
using Domain.Common;
using Domain.Entities.Organizacion;
using FluentValidation;
using MediatR;

namespace Application.Features.Organizacion.Cargos;

/// <param name="TitularDesde">Desde cuándo ocupa el cargo el titular; por defecto, ahora.</param>
public record CreateCargoCommand(string Nombre, Guid DependenciaId, Guid? TitularId, DateTime? TitularDesde = null) : IRequest<Result<Guid>>;

public class CreateCargoCommandValidator : AbstractValidator<CreateCargoCommand>
{
    public CreateCargoCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DependenciaId).NotEmpty();
        RuleFor(x => x.TitularDesde).LessThanOrEqualTo(_ => DateTime.Now)
            .WithMessage("La fecha de inicio del titular no puede ser futura.");
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

        cargo.CambiarTitular(request.TitularId, request.TitularDesde ?? DateTime.Now);

        await uow.AddAsync(cargo, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(cargo.Id);
    }
}
