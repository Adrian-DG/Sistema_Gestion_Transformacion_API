using Application.Contracts.Externos;
using Domain.Common;
using FluentValidation;
using MediatR;

namespace Application.Features.Externos;

public record GetMiembroByCedulaQuery(string Cedula) : IRequest<Result<MiembroExternoDto>>;

public record BuscarCargosExternosQuery(string Nombre, int MaxResultados = 20) : IRequest<Result<IReadOnlyList<CargoExternoDto>>>;

public class GetMiembroByCedulaQueryValidator : AbstractValidator<GetMiembroByCedulaQuery>
{
    public GetMiembroByCedulaQueryValidator()
    {
        RuleFor(x => x.Cedula)
            .NotEmpty()
            .Matches(@"^\d{3}-?\d{7}-?\d$")
            .WithMessage("La cédula debe tener el formato 000-0000000-0.");
    }
}

public class BuscarCargosExternosQueryValidator : AbstractValidator<BuscarCargosExternosQuery>
{
    public BuscarCargosExternosQueryValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.MaxResultados).InclusiveBetween(1, 100);
    }
}

public class SipffaaQueryHandler(ISipffaaService sipffaa) :
    IRequestHandler<GetMiembroByCedulaQuery, Result<MiembroExternoDto>>,
    IRequestHandler<BuscarCargosExternosQuery, Result<IReadOnlyList<CargoExternoDto>>>
{
    public async Task<Result<MiembroExternoDto>> Handle(GetMiembroByCedulaQuery request, CancellationToken cancellationToken)
    {
        var miembro = await sipffaa.GetMiembroByCedulaAsync(request.Cedula, cancellationToken);

        return miembro is not null
            ? Result.Success(miembro)
            : Result.Failure<MiembroExternoDto>(Error.NotFound(
                "Sipffaa.MiembroNotFound", $"No se encontró un miembro con la cédula '{request.Cedula}'."));
    }

    public async Task<Result<IReadOnlyList<CargoExternoDto>>> Handle(BuscarCargosExternosQuery request, CancellationToken cancellationToken)
    {
        var cargos = await sipffaa.BuscarCargosAsync(request.Nombre, request.MaxResultados, cancellationToken);
        return Result.Success(cargos);
    }
}
