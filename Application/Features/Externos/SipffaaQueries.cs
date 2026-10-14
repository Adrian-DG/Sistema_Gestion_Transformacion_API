using Application.Contracts.Externos;
using Domain.Common;
using FluentValidation;
using MediatR;

namespace Application.Features.Externos;

public record GetMiembroByCedulaQuery(string Cedula) : IRequest<Result<MiembroExternoDto>>;

/// <summary>Busca miembros activos por inicio de cédula o por nombre (autocompletado).</summary>
public record BuscarMiembrosExternosQuery(string Texto, int MaxResultados = 20) : IRequest<Result<IReadOnlyList<MiembroExternoDto>>>;

public record BuscarCargosExternosQuery(string Nombre, int MaxResultados = 20) : IRequest<Result<IReadOnlyList<CargoExternoDto>>>;

/// <summary>Departamentos (dependencias) que solicitan asignaciones de vehículos o tickets de combustible.</summary>
public record BuscarDepartamentosExternosQuery(string Nombre, int MaxResultados = 20) : IRequest<Result<IReadOnlyList<DepartamentoExternoDto>>>;

public record GetDepartamentoExternoQuery(int CodDepartamento) : IRequest<Result<DepartamentoExternoDto>>;

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

public class BuscarMiembrosExternosQueryValidator : AbstractValidator<BuscarMiembrosExternosQuery>
{
    public BuscarMiembrosExternosQueryValidator()
    {
        RuleFor(x => x.Texto).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.MaxResultados).InclusiveBetween(1, 100);
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

public class BuscarDepartamentosExternosQueryValidator : AbstractValidator<BuscarDepartamentosExternosQuery>
{
    public BuscarDepartamentosExternosQueryValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.MaxResultados).InclusiveBetween(1, 100);
    }
}

public class GetDepartamentoExternoQueryValidator : AbstractValidator<GetDepartamentoExternoQuery>
{
    public GetDepartamentoExternoQueryValidator() => RuleFor(x => x.CodDepartamento).GreaterThan(0);
}

public class SipffaaQueryHandler(ISipffaaService sipffaa) :
    IRequestHandler<GetMiembroByCedulaQuery, Result<MiembroExternoDto>>,
    IRequestHandler<BuscarMiembrosExternosQuery, Result<IReadOnlyList<MiembroExternoDto>>>,
    IRequestHandler<BuscarCargosExternosQuery, Result<IReadOnlyList<CargoExternoDto>>>,
    IRequestHandler<BuscarDepartamentosExternosQuery, Result<IReadOnlyList<DepartamentoExternoDto>>>,
    IRequestHandler<GetDepartamentoExternoQuery, Result<DepartamentoExternoDto>>
{
    public async Task<Result<MiembroExternoDto>> Handle(GetMiembroByCedulaQuery request, CancellationToken cancellationToken)
    {
        var miembro = await sipffaa.GetMiembroByCedulaAsync(request.Cedula, cancellationToken);

        return miembro is not null
            ? Result.Success(miembro)
            : Result.Failure<MiembroExternoDto>(Error.NotFound(
                "Sipffaa.MiembroNotFound", $"No se encontró un miembro con la cédula '{request.Cedula}'."));
    }

    public async Task<Result<IReadOnlyList<MiembroExternoDto>>> Handle(BuscarMiembrosExternosQuery request, CancellationToken cancellationToken)
    {
        var miembros = await sipffaa.BuscarMiembrosAsync(request.Texto, request.MaxResultados, cancellationToken);
        return Result.Success(miembros);
    }

    public async Task<Result<IReadOnlyList<CargoExternoDto>>> Handle(BuscarCargosExternosQuery request, CancellationToken cancellationToken)
    {
        var cargos = await sipffaa.BuscarCargosAsync(request.Nombre, request.MaxResultados, cancellationToken);
        return Result.Success(cargos);
    }

    public async Task<Result<IReadOnlyList<DepartamentoExternoDto>>> Handle(BuscarDepartamentosExternosQuery request, CancellationToken cancellationToken)
    {
        var departamentos = await sipffaa.BuscarDepartamentosAsync(request.Nombre, request.MaxResultados, cancellationToken);
        return Result.Success(departamentos);
    }

    public async Task<Result<DepartamentoExternoDto>> Handle(GetDepartamentoExternoQuery request, CancellationToken cancellationToken)
    {
        var departamento = await sipffaa.GetDepartamentoAsync(request.CodDepartamento, cancellationToken);

        return departamento is not null
            ? Result.Success(departamento)
            : Result.Failure<DepartamentoExternoDto>(Error.NotFound(
                "Sipffaa.DepartamentoNotFound", $"No se encontró el departamento con código '{request.CodDepartamento}'."));
    }
}
