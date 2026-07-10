using Application.Contracts;
using Domain.Entities.Historico;
using FluentValidation;
using MediatR;

namespace Application.Features.Historico.Polizas;

public record CreatePolizaCommand(
    Guid VehiculoId,
    DateOnly FechaExpedicion,
    DateOnly FechaEfectividad,
    DateOnly FechaVencimiento
) : IRequest<Guid>;

public class CreatePolizaCommandValidator : AbstractValidator<CreatePolizaCommand>
{
    public CreatePolizaCommandValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.FechaVencimiento)
            .GreaterThan(x => x.FechaEfectividad)
            .WithMessage("La fecha de vencimiento debe ser mayor a la fecha de efectividad.");
    }
}

public class CreatePolizaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreatePolizaCommand, Guid>
{
    public async Task<Guid> Handle(CreatePolizaCommand request, CancellationToken cancellationToken)
    {
        _ = await unitOfWork.VehiculoRepository.GetById(request.VehiculoId, cancellationToken);

        var poliza = new Poliza
        {
            VehiculoId = request.VehiculoId,
            FechaExpedicion = request.FechaExpedicion,
            FechaEfectividad = request.FechaEfectividad,
            FechaVencimiento = request.FechaVencimiento,
        };

        await unitOfWork.PolizaRepository.Create(poliza, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return poliza.Id;
    }
}
