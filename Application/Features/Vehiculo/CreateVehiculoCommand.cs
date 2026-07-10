using Application.Contracts;
using Domain.Common;
using FluentValidation;
using MediatR;

namespace Application.Features.Vehiculo;

public record CreateVehiculoCommand(
    string Chasis,
    string Matricula,
    string Placa,
    int Fabricacion,
    Guid MarcaId,
    Guid ModeloId,
    Guid TipoId,
    Guid ColorId
) : IRequest<Result>;

public class CreateVehiculoCommandValidator : AbstractValidator<CreateVehiculoCommand>
{
    public CreateVehiculoCommandValidator()
    {
        RuleFor(x => x.Chasis)
            .NotEmpty().WithMessage("El chasis es obligatorio.");

        RuleFor(x => x.Matricula)
            .NotEmpty().WithMessage("La matrícula es obligatoria.");

        RuleFor(x => x.Placa)
            .NotEmpty().WithMessage("La placa es obligatoria.")
            .MaximumLength(10).WithMessage("La placa debe tener un máximo de 10 caracteres.");

        RuleFor(x => x.Fabricacion)
            .InclusiveBetween(1900, DateTime.Now.Year + 1)
            .WithMessage($"El año de fabricación debe estar entre 1900 y {DateTime.Now.Year + 1}.");

        RuleFor(x => x.MarcaId)
            .NotEmpty().WithMessage("La marca es obligatoria.");

        RuleFor(x => x.ModeloId)
            .NotEmpty().WithMessage("El modelo es obligatorio.");

        RuleFor(x => x.TipoId)
            .NotEmpty().WithMessage("El tipo de vehículo es obligatorio.");

        RuleFor(x => x.ColorId)
            .NotEmpty().WithMessage("El color es obligatorio.");
    }
}

public class CreateVehiculoCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateVehiculoCommand, Result>
{
    public async Task<Result> Handle(CreateVehiculoCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = new Domain.Entities.Recursos.Vehiculo
        {
            Chasis = request.Chasis,
            Matricula = request.Matricula,
            Placa = request.Placa,
            Fabricacion = request.Fabricacion,
            MarcaId = request.MarcaId,
            ModeloId = request.ModeloId,
            TipoId = request.TipoId,
            ColorId = request.ColorId,
            Disponible = true
        };

        await uow.VehiculoRepository.Create(vehiculo, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
