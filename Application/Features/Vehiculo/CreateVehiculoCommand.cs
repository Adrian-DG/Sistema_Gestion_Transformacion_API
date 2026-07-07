using Application.Contracts;
using AutoMapper;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Vehiculo;

public record CreateVehiculoCommand(
    string Chasis,
    string Matricula,
    string Placa,
    int Fabricacion,
    Guid Marca,
    Guid Modelo,
    Guid Tipo,
    Guid Color    
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
        RuleFor(x => x.Marca).NotEmpty().WithMessage("La marca es obligatoria.");
        RuleFor(x => x.Modelo).NotEmpty().WithMessage("El modelo es obligatorio.");
        RuleFor(x => x.Tipo).NotEmpty().WithMessage("El tipo de vehículo es obligatorio.");
        RuleFor(x => x.Color).NotEmpty().WithMessage("El color es obligatorio.");
    }
}


public class CreateVehiculoCommandHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<CreateVehiculoCommand, Unit>
{
    public async Task<Unit> Handle(CreateVehiculoCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = mapper.Map<Domain.Entities.Recursos.Vehiculo>(request);
        await uow.VehiculoRepository.Create(vehiculo);
        return Unit.Value;
    }
}
