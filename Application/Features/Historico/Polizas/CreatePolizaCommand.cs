using Application.Contracts;
using Domain.Common;
using Domain.Entities.Historico;
using FluentValidation;
using MediatR;

namespace Application.Features.Historico.Polizas;

public record CreatePolizaCommand(
    Guid VehiculoId,
    Guid AseguradoraId,
    string Numero,
    DateOnly FechaExpedicion,
    DateOnly FechaEfectividad,
    DateOnly FechaVencimiento
) : IRequest<Result<Guid>>;

public class CreatePolizaCommandValidator : AbstractValidator<CreatePolizaCommand>
{
    public CreatePolizaCommandValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.AseguradoraId).NotEmpty();
        RuleFor(x => x.Numero).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FechaVencimiento)
            .GreaterThan(x => x.FechaEfectividad)
            .WithMessage("La fecha de vencimiento debe ser mayor a la fecha de efectividad.");
    }
}

public class CreatePolizaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreatePolizaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePolizaCommand request, CancellationToken cancellationToken)
    {
        var vehiculoResult = await unitOfWork.VehiculoRepository.GetById(request.VehiculoId, cancellationToken);

        if (vehiculoResult.IsFailure)
        {
            return Result.Failure<Guid>(vehiculoResult.Error);
        }

        var poliza = new Poliza
        {
            VehiculoId = request.VehiculoId,
            AseguradoraId = request.AseguradoraId,
            Numero = request.Numero,
            FechaExpedicion = request.FechaExpedicion,
            FechaEfectividad = request.FechaEfectividad,
            FechaVencimiento = request.FechaVencimiento,
        };

        await unitOfWork.PolizaRepository.Create(poliza, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return poliza.Id;
    }
}
