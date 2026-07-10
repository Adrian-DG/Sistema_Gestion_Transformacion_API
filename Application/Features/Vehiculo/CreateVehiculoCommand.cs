using Application.Contracts;
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
) : IRequest<Unit>;

public class CreateVehiculoCommandValidator : AbstractValidator<CreateVehiculoCommand>
{
    public CreateVehiculoCommandValidator()
    {
        RuleFor(x => x.Chasis).NotEmpty().WithMessage("El chasis es obligatorio.");
        RuleFor(x => x.Matricula).NotEmpty().WithMessage("La matrícula es obligatoria.");

        RuleFor(x => x.Placa)
            .Must(x => !string.IsNullOrWhiteSpace(x) && x.Length <= 10)
            .WithMessage("La placa es obligatoria y debe tener un máximo de 10 caracteres.");

        RuleFor(x => x.Fabricacion).GreaterThan(0).WithMessage("El año de fabricación debe ser mayor que cero.");
        RuleFor(x => x.MarcaId).NotEmpty().WithMessage("La marca es obligatoria.");
        RuleFor(x => x.ModeloId).NotEmpty().WithMessage("El modelo es obligatorio.");
        RuleFor(x => x.TipoId).NotEmpty().WithMessage("El tipo de vehículo es obligatorio.");
        RuleFor(x => x.ColorId).NotEmpty().WithMessage("El color es obligatorio.");
    }
}

public class CreateVehiculoCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateVehiculoCommand, Unit>
{
    public async Task<Unit> Handle(CreateVehiculoCommand request, CancellationToken cancellationToken)
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
        return Unit.Value;
    }
}
